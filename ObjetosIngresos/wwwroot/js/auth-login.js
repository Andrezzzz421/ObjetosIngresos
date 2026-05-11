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
    const doc = document.getElementById("documento").value;
    const pass = document.getElementById("password").value;
    const errorDiv = document.getElementById("mensajeError");

    if (!doc || !pass) {
        mostrarError("Por favor, llena todos los campos.");
        return;
    }

    setLoading(true);
    if (errorDiv) errorDiv.classList.add("d-none");

    try {
        const emailSintetico = `${doc}@sistema.com`;
        await signInWithEmailAndPassword(auth, emailSintetico, pass);


        const resSrv = await fetch(`/Auth/GenerarSesionSrv?documento=${encodeURIComponent(doc)}`, {
            method: 'POST'
        });

        if (resSrv.ok) {
            window.location.replace("/Auth/Perfil");
        } else {
            const errorSrv = await resSrv.text();
            mostrarError("Error: " + errorSrv);
            setLoading(false);
        }

    } catch (error) {
        console.error("Error Firebase:", error.code);
        if ((error.code === 'auth/user-not-found' || error.code === 'auth/invalid-credential') && doc === pass) {
            await vincularUsuario(doc);
        } else {
            mostrarError("Credenciales incorrectas o usuario no vinculado.");
            setLoading(false);
        }
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
            mostrarError("Error del servidor: " + txt);
            setLoading(false);
        }
    } catch (err) {
        mostrarError("No se pudo conectar con el servidor.");
        setLoading(false);
    }
}

async function cerrarSesion() {
    if (!confirm("¿Estás seguro de que quieres salir?")) return;

    try {
        await signOut(auth);

        const response = await fetch('/Auth/LogoutServidor', {
            method: 'POST'
        });

        if (response.ok) {
            window.location.replace("/Auth/Login");  
        } else {
            alert("Error al limpiar la sesión en el servidor.");
        }
    } catch (error) {
        console.error("Error al salir:", error);
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