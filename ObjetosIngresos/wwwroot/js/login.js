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

window.procesarLogin = async () => {
    const documento = document.getElementById("documento").value;
    const password = document.getElementById("password").value;

    const emailSintetico = `${documento}@sistema.com`;

    try {
        await signInWithEmailAndPassword(auth, emailSintetico, password);
        window.location.href = "/Usuarios/Perfil";

    } catch (error) {
        if ((error.code === 'auth/user-not-found' || error.code === 'auth/invalid-credential') && documento === password) {

            const response = await fetch('/Auth/VincularPrimerIngreso', {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: `documento=${documento}`
            });

            if (response.ok) {
                alert("Primer ingreso. Por seguridad, cambia tu contraseña.");
                window.location.href = `/Usuarios/CompletarRegistro?documento=${documento}`;
            } else {
                alert("Error al vincular el usuario en la base de datos.");
            }
        } else {
            alert("Credenciales incorrectas.");
        }
    }
};

window.actualizarContrasena = async (documento) => {
    clearMessage("updateMessage");

    const p1 = document.getElementById("pass1").value;
    const p2 = document.getElementById("pass2").value;

    if (p1.length < 6) return showMessage("updateMessage", "Mínimo 6 caracteres.");
    if (p1 !== p2) return showMessage("updateMessage", "Las contraseñas no coinciden.");

    try {
        const emailSintetico = `${documento}@sistema.com`;
        const userCredential = await signInWithEmailAndPassword(auth, emailSintetico, documento);
        await updatePassword(userCredential.user, p1);

        alert("¡Contraseña actualizada!");
        window.location.href = "/Usuarios/Perfil";
    } catch (error) {
        alert("Error: " + error.message);
    }
};