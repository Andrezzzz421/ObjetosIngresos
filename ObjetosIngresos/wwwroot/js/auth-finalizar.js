document.addEventListener("DOMContentLoaded", () => {
    const btn = document.getElementById("btnActualizar");
    if (btn) {
        btn.addEventListener("click", async () => {
            await finalizarRegistro();
        });
    }
});

async function finalizarRegistro() {
    const email = document.getElementById("userEmail").value;
    const p1 = document.getElementById("newPass").value;
    const p2 = document.getElementById("confirmPass").value;
    const errorDiv = document.getElementById("mensajeError");

    if (errorDiv) errorDiv.classList.add("hidden");

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
        const response = await fetch('/Auth/ActualizarPassword', {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: `email=${encodeURIComponent(email)}&password=${encodeURIComponent(p1)}&confirmarPassword=${encodeURIComponent(p2)}`
        });

        if (response.ok) {
            await Swal.fire({
                icon: 'success',
                title: '¡Seguridad Configurada!',
                text: 'Tu contraseña ha sido guardada. Ahora puedes iniciar sesión.',
                confirmButtonColor: '#4f46e5',
                customClass: {
                    popup: 'rounded-3xl shadow-2xl',
                    title: 'text-slate-800 font-bold'
                }
            });

            window.location.replace("/Auth/Login");
        } else {
            const txt = await response.text();
            mostrarError(txt || "El servidor rechazó la solicitud.");
            setLoading(false);
        }

    } catch (error) {
        console.error("Error al actualizar password:", error);
        mostrarError("No se pudo conectar con el servidor central.");
        setLoading(false);
    }
}

function setLoading(isLoading) {
    const btn = document.getElementById("btnActualizar");
    const spinner = document.getElementById("btnSpinner");
    const text = document.getElementById("btnText");
    if (!btn) return;

    btn.disabled = isLoading;
    if (isLoading) {
        spinner?.classList.remove("hidden");
        if (text) text.innerText = "Actualizando credenciales...";
    } else {
        spinner?.classList.add("hidden");
        if (text) text.innerText = "Guardar y Finalizar";
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