import { initializeApp } from "https://www.gstatic.com/firebasejs/10.8.0/firebase-app.js";
import { getAuth, signInWithEmailAndPassword, updatePassword } from "https://www.gstatic.com/firebasejs/10.8.0/firebase-auth.js";

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

window.finalizarRegistro = async (documento) => {
    const p1 = document.getElementById("newPass").value;
    const p2 = document.getElementById("confirmPass").value;
    const errorDiv = document.getElementById("mensajeError");
    const btn = document.getElementById("btnActualizar");

    // 1. Validaciones
    if (p1.length < 6) {
        mostrarError("La contraseña debe tener al menos 6 caracteres.");
        return;
    }
    if (p1 !== p2) {
        mostrarError("Las contraseñas no coinciden.");
        return;
    }

    setLoading(true);

    try {
        const emailSintetico = `${documento}@sistema.com`;

        const userCredential = await signInWithEmailAndPassword(auth, emailSintetico, documento);

        await updatePassword(userCredential.user, p1);

        alert("¡Seguridad configurada correctamente! Bienvenido.");
        window.location.href = "/Auth/Perfil";

    } catch (error) {
        console.error(error);
        mostrarError("Error al actualizar seguridad: " + traducirError(error.code));
        setLoading(false);
    }
};

function setLoading(isLoading) {
    const btn = document.getElementById("btnActualizar");
    const spinner = document.getElementById("btnSpinner");
    const text = document.getElementById("btnText");
    btn.disabled = isLoading;
    if (isLoading) {
        spinner.classList.remove("d-none");
        text.innerText = "Guardando...";
    } else {
        spinner.classList.add("d-none");
        text.innerText = "Guardar y Finalizar";
    }
}

function mostrarError(msg) {
    const errorDiv = document.getElementById("mensajeError");
    errorDiv.innerText = msg;
    errorDiv.classList.remove("d-none");
}

function traducirError(code) {
    switch (code) {
        case 'auth/requires-recent-login': return "Por seguridad, vuelve a intentar el proceso.";
        case 'auth/weak-password': return "La contraseña es muy débil.";
        default: return "Ocurrió un error inesperado.";
    }
}