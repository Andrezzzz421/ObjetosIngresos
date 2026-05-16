import { initializeApp } from "https://www.gstatic.com/firebasejs/10.8.0/firebase-app.js";
import {
    getAuth,
    signInWithEmailAndPassword,
    signOut
} from "https://www.gstatic.com/firebasejs/10.8.0/firebase-auth.js";

const firebaseConfig = {
    apiKey: "AIzaSyC4JR7oVCvHY8fYkdi276azfdDS_Q6hL10",
    authDomain: "sistemaingreso.firebaseapp.com",
    projectId: "sistemaingreso",
    storageBucket: "sistemaingreso.firebasestorage.app",
    messagingSenderId: "266766184234",
    appId: "1:266766184234:web:475e50654bb5ad8ed8f853"
};


const app = initializeApp(firebaseConfig);
const auth = getAuth(app);

async function procesarLogin() {
    const identificador = document.getElementById("documento").value;
    const pass = document.getElementById("password").value;
    const errorDiv = document.getElementById("mensajeError");

    if (!identificador || !pass) {
        mostrarError("Por favor, llena todos los campos.");
        return;
    }

    setLoading(true);
    if (errorDiv) errorDiv.classList.add("d-none");

    try {
        const resUser = await fetch(`/Auth/ObtenerDatosUsuario?identificador=${encodeURIComponent(identificador)}`);

        if (!resUser.ok) {
            throw new Error("El usuario no pertenece a la institución.");
        }

        const datos = await resUser.json(); 


        if (datos.necesitaVinculacion && identificador === pass) {
            await vincularUsuario(identificador);
            return;
        }


        await signInWithEmailAndPassword(auth, datos.correo, pass);

        const resSrv = await fetch(`/Auth/GenerarSesionSrv?identificador=${encodeURIComponent(identificador)}`, {
            method: 'POST'
        });

        if (resSrv.ok) {
            window.location.replace("/Auth/Perfil");
        } else {
            mostrarError("Error al validar sesión local.");
            setLoading(false);
        }

    } catch (error) {
        console.error("Error Login:", error);
        let mensaje = "Credenciales incorrectas o usuario no vinculado.";

        if (error.message.includes("institución")) mensaje = error.message;
        if (error.code === 'auth/wrong-password') mensaje = "Contraseña incorrecta.";

        mostrarError(mensaje);
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
            console.error("Detalle del error 400:", txt);
            mostrarError(txt);
            setLoading(false);
        }
    } catch (err) {
        mostrarError("Error de conexión al vincular.");
        setLoading(false);
    }
}

async function cerrarSesion() {
    const result = await Swal.fire({
        title: '¿Cerrar sesión?',
        text: "Tendrás que ingresar tus credenciales nuevamente.",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#4f46e5',
        cancelButtonColor: '#64748b',  
        confirmButtonText: 'Sí, salir',
        cancelButtonText: 'Cancelar',
        background: '#ffffff',
        borderRadius: '1.5rem', 
        customClass: {
            popup: 'rounded-3xl shadow-2xl border border-slate-100',
            title: 'text-slate-800 font-bold',
            confirmButton: 'rounded-xl px-6 py-3 font-bold',
            cancelButton: 'rounded-xl px-6 py-3 font-bold'
        }
    });

    if (result.isConfirmed) {
        try {
            Swal.showLoading();

            await signOut(auth);
            const response = await fetch('/Auth/LogoutServidor', { method: 'POST' });

            if (response.ok) {
                window.location.replace("/Auth/Login");
            } else {
                Swal.fire('Error', 'No se pudo cerrar la sesión en el servidor.', 'error');
            }
        } catch (error) {
            console.error("Error al salir:", error);
            Swal.fire('Error', 'Ocurrió un error inesperado.', 'error');
        }
    }
}

function setLoading(isLoading) {
    const btn = document.getElementById("btnLogin");
    const spinner = document.getElementById("btnSpinner");
    const text = document.getElementById("btnText");
    if (!btn) return;

    if (isLoading) {
        btn.disabled = true;
        spinner?.classList.remove("d-none");
        if (text) text.innerText = "Verificando...";
    } else {
        btn.disabled = false;
        spinner?.classList.add("d-none");
        if (text) text.innerText = "Entrar al Sistema";
    }
}

function mostrarError(msg) {
    const errorDiv = document.getElementById("mensajeError");
    if (errorDiv) {
        errorDiv.innerText = msg;
        errorDiv.classList.remove("d-none");
    } else {
        alert(msg);
    }
}
 

window.procesarLogin = procesarLogin;
window.cerrarSesion = cerrarSesion;