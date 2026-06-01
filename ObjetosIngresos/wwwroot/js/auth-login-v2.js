import { initializeApp } from "https://www.gstatic.com/firebasejs/10.8.0/firebase-app.js";
import { getAuth, signInWithEmailAndPassword } from "https://www.gstatic.com/firebasejs/10.8.0/firebase-auth.js";

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

    // FASE 1: Obtención de datos del usuario
    try {
        const resUser = await fetch(`/Auth/ObtenerDatosUsuario?identificador=${encodeURIComponent(correoIngresado)}`);
        if (!resUser.ok) throw new Error("El usuario no pertenece a la institución.");

        datosServer = await resUser.json();
    } catch (error) {
        console.error("Error en Fase 1:", error);
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

        const resSrv = await fetch(`/Auth/GenerarSesionSrv?identificador=${encodeURIComponent(correoIngresado)}`, {
            method: 'POST'
        });

        if (!resSrv.ok) throw new Error("Error al validar sesión local.");

        window.location.replace("/Auth/Perfil");

    } catch (error) {
        console.error("Error en Fase 2/3:", error);
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

window.procesarLoginInvitado = procesarLoginInvitado;
window.procesarLogin = procesarLogin;