import { initializeApp } from "https://www.gstatic.com/firebasejs/10.8.0/firebase-app.js";
import { getAuth, signInWithEmailAndPassword,updatePassword } from "https://www.gstatic.com/firebasejs/10.8.0/firebase-auth.js";

const firebaseConfig = window.FirebaseConfigServidor;
let app;
let auth;

if (!firebaseConfig || !firebaseConfig.apiKey) {
    console.warn("AVISO: No se detectaron configuraciones de Firebase en esta vista.");
} else {
    app = initializeApp(firebaseConfig);
    auth = getAuth(app);
}

async function procesarLogin() {
    const userInput = document.getElementById("documento");
    const passInput = document.getElementById("password");
    const errorDiv = document.getElementById("mensajeError");

    const correoIngresado = userInput.value.trim();
    const passIngresada = passInput.value.trim();

    limpiarEstilosErrores();

    if (!correoIngresado || !passIngresada) {
        mostrarError("Por favor, llena todos los campos.");
        if (!correoIngresado) userInput.classList.add("border-red-500", "is-invalid");
        if (!passIngresada) passInput.classList.add("border-red-500", "is-invalid");
        return;
    }

    setLoading(true);
    if (errorDiv) errorDiv.classList.add("hidden");

    let datosServer = null;
    try {
        const resUser = await fetch(`/Auth/ObtenerDatosUsuario?identificador=${encodeURIComponent(correoIngresado)}`);
        if (!resUser.ok) throw new Error("El usuario no pertenece a la institución.");

        datosServer = await resUser.json();
    } catch (error) {
        mostrarError(error.message || "Error al conectar con el servidor.");
        userInput.classList.add("border-red-500", "is-invalid");
        setLoading(false);
        return;
    }

    try {
        if (datosServer.necesitaVinculacion) {
            if (passIngresada === datosServer.documento.trim()) {
                await vincularUsuario(datosServer.documento.trim());
                return;
            } else {
                throw new Error("Contraseña incorrecta. Para su primer ingreso, su contraseña es su número de documento.");
            }
        }

        await signInWithEmailAndPassword(auth, datosServer.correo, passIngresada);

        const resSrv = await fetch(`/Auth/GenerarSesionSrv?documento=${encodeURIComponent(correoIngresado)}`, {
            method: 'POST'
        });

        if (!resSrv.ok) throw new Error("Error al validar sesión local.");

        const datosRedir = await resSrv.json();
        window.location.replace(datosRedir.redirectUrl ?? "/MiPanel");

    } catch (error) {
        let mensaje = "Credenciales incorrectas o usuario no vinculado.";
        let esErrorPassword = false;

        if (error.message.includes("Para su primer ingreso") || error.code === 'auth/wrong-password' || error.code === 'auth/invalid-credential') {
            mensaje = error.code ? "Contraseña incorrecta. Por favor, inténtelo de nuevo." : error.message;
            esErrorPassword = true;
        } else if (error.code === 'auth/too-many-requests') {
            mensaje = "Demasiados intentos fallidos. Cuenta bloqueada temporalmente.";
        } else if (error.message.includes("sesión local")) {
            mensaje = error.message;
        }

        mostrarError(mensaje);
        if (esErrorPassword && passInput) passInput.classList.add("border-red-500", "focus:ring-red-500");
        setLoading(false);
    }
}

async function vincularUsuario(doc) {
    try {
        const response = await fetch('/Auth/VincularPrimerIngreso', {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: `documento=${encodeURIComponent(doc)}`
        });

        if (response.ok) {
            window.location.href = `/Auth/CompletarRegistro?documento=${doc}`;
        } else {
            const txt = await response.text();
            mostrarError(txt);
            setLoading(false);
        }
    } catch (err) {
        mostrarError("Error de conexión al vincular.");
        setLoading(false);
    }
}

function setLoading(isLoading) {
    const btn = document.getElementById("btnLogin");
    const spinner = document.getElementById("btnSpinner");
    const text = document.getElementById("btnText");
    if (!btn) return;

    btn.disabled = isLoading;
    if (isLoading) {
        spinner?.classList.remove("hidden");
        if (text) text.innerText = "Verificando...";
    } else {
        spinner?.classList.add("hidden");
        if (text) text.innerText = "Entrar al Sistema";
    }
}

function mostrarError(msg) {
    const errorDiv = document.getElementById("mensajeError");
    if (errorDiv) {
        errorDiv.innerText = msg;
        errorDiv.classList.remove("hidden");
    } else {
        alert(msg);
    }
}

function limpiarEstilosErrores() {
    const errorDiv = document.getElementById("mensajeError");
    const userInput = document.getElementById("documento");
    const passInput = document.getElementById("password");

    if (errorDiv) errorDiv.classList.add("hidden");
    if (userInput) userInput.classList.remove("border-red-500", "border-danger", "is-invalid");
    if (passInput) passInput.classList.remove("border-red-500", "border-danger", "is-invalid", "focus:ring-red-500");
}

async function procesarLoginInvitado() {
    const btn = document.getElementById("btnInvitado");
    if (btn) btn.disabled = true;

    try {
        const response = await fetch('/Auth/LoginInvitado', {
            method: 'POST'
        });

        if (response.ok) {
            window.location.replace("/Home/Index");
        } else {
            mostrarError("No se pudo iniciar sesión como invitado.");
            if (btn) btn.disabled = false;
        }
    } catch (error) {
        console.error("Error en Login Invitado:", error);
        mostrarError("Error de red al intentar conectar como invitado.");
        if (btn) btn.disabled = false;
    }
}

window.finalizarRegistro = async (documento) => {
    const pass1 = document.getElementById("newPass");
    const pass2 = document.getElementById("confirmPass");
    const errorDiv = document.getElementById("mensajeError");

    const p1Value = pass1.value.trim();
    const p2Value = pass2.value.trim();

    if (errorDiv) errorDiv.classList.add("hidden");
    pass1.classList.remove("border-red-500");
    pass2.classList.remove("border-red-500");

    if (p1Value.length < 6) {
        mostrarErrorRegistro("La contraseña debe tener al menos 6 caracteres.");
        pass1.classList.add("border-red-500");
        return;
    }

    if (p1Value !== p2Value) {
        mostrarErrorRegistro("Las contraseñas no coinciden.");
        pass2.classList.add("border-red-500");
        return;
    }

    setLoadingRegistro(true);

    try {
        const resUser = await fetch(`/Auth/ObtenerDatosUsuario?identificador=${encodeURIComponent(documento)}`);
        if (!resUser.ok) throw new Error("No se pudieron recuperar las credenciales del usuario.");

        const datosServer = await resUser.json();

        const userCredential = await signInWithEmailAndPassword(auth, datosServer.correo, documento);

        await updatePassword(userCredential.user, p1Value);
        const resSrv = await fetch(`/Auth/GenerarSesionSrv?documento=${encodeURIComponent(documento)}`, {
            method: 'POST'
        });

        if (!resSrv.ok) throw new Error("Error al inicializar la sesión en el servidor.");

        const datosRedir2 = await resSrv.json();

        const modal = document.getElementById("modalExito");
        if (modal) {
            modal.classList.remove("hidden");
            setTimeout(() => {
                modal.classList.remove("opacity-0");
                modal.querySelector(".transform").classList.remove("scale-95");
            }, 10);
        } else {
            alert("¡Contraseña configurada con éxito! Bienvenido al sistema.");
            window.location.replace("/Auth/Perfil");
        }
        window.location.replace("/Auth/Perfil");

    } catch (error) {
        console.error("Error en la finalización del registro:", error);
        let msg = "No se pudo actualizar la contraseña. Si ya habías hecho este proceso, intenta iniciar sesión normalmente.";

        if (error.code === "auth/requires-recent-login") {
            msg = "La sesión expiró. Por favor, vuelve al Login e ingresa tu documento.";
        } else if (error.message) {
            msg = error.message;
        }

        mostrarErrorRegistro(msg);
        setLoadingRegistro(false);
    }
};

function setLoadingRegistro(isLoading) {
    const btn = document.getElementById("btnActualizar");
    const spinner = document.getElementById("btnSpinner");
    const text = document.getElementById("btnText");
    if (!btn) return;

    btn.disabled = isLoading;
    if (isLoading) {
        spinner?.classList.remove("hidden");
        if (text) text.innerText = "Guardando cambios...";
    } else {
        spinner?.classList.add("hidden");
        if (text) text.innerText = "Guardar y Finalizar";
    }
}

function mostrarErrorRegistro(msg) {
    const errorDiv = document.getElementById("mensajeError");
    if (errorDiv) {
        errorDiv.innerText = msg;
        errorDiv.classList.remove("hidden");
    } else {
        alert(msg);
    }
}


window.procesarLoginInvitado = procesarLoginInvitado;
window.procesarLogin = procesarLogin;