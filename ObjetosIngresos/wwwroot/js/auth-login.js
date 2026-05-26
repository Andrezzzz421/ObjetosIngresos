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