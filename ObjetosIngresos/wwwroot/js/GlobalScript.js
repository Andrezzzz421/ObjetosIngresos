// Perfil.cshtml
window.hideToast = function() {
    const toast = document.getElementById("toastError");
    if (toast) {
        toast.classList.add("opacity-0", "translate-y-10");
        setTimeout(() => toast.classList.add("hidden"), 300);
    }
};

// NuevaPassword.cshtml
window.confirmarCambio = function() {
    const pass = document.getElementById('password').value;
    const confirmPass = document.getElementById('confirmarPassword').value;

    if (pass === "" || confirmPass === "") {
        Swal.fire({
            icon: 'warning',
            title: 'Campos vacíos',
            text: 'Por favor completa ambos campos.',
            confirmButtonColor: '#4f46e5',
            customClass: { popup: 'rounded-3xl' }
        });
        return;
    }

    if (pass !== confirmPass) {
        Swal.fire({
            icon: 'error',
            title: 'No coinciden',
            text: 'Las contraseñas ingresadas no son iguales.',
            confirmButtonColor: '#4f46e5',
            customClass: { popup: 'rounded-3xl' }
        });
        return;
    }
     
    Swal.fire({
        title: '¿Estás seguro?',
        text: "Se cerrará tu sesión actual para aplicar los cambios.",
        icon: 'question',
        showCancelButton: true,
        confirmButtonColor: '#4f46e5',
        cancelButtonColor: '#f1f5f9',
        confirmButtonText: 'Sí, cambiar',
        cancelButtonText: '<span style="color: #64748b">Cancelar</span>',
        reverseButtons: true,
        customClass: {
            popup: 'rounded-[2rem]',
            confirmButton: 'rounded-xl px-6 py-3 font-bold',
            cancelButton: 'rounded-xl px-6 py-3 font-bold'
        }
    }).then((result) => {
        if (result.isConfirmed) { 
            Swal.fire({
                title: 'Procesando...',
                html: 'Actualizando credenciales en Firebase',
                allowOutsideClick: false,
                didOpen: () => { Swal.showLoading() }
            });

            document.getElementById('formCambioPass').submit();
        }
    });
};

// Login.cshtml
window.togglePassword = function() {
    const passwordInput = document.getElementById('password');
    const eyeOpen = document.getElementById('eye-open');
    const eyeClosed = document.getElementById('eye-closed');

    if (passwordInput.type === 'password') {
        passwordInput.type = 'text';
        eyeOpen.classList.add('hidden');
        eyeClosed.classList.remove('hidden');
    } else {
        passwordInput.type = 'password';
        eyeOpen.classList.remove('hidden');
        eyeClosed.classList.add('hidden');
    }
};

// CompletarRegistro.cshtml
window.despacharRegistro = function() {
    const docValue = document.getElementById("identificadorUsuario").value;

    if (!docValue) {
        console.error("Error: El documento del usuario llegó vacío a la vista.");
        return;
    }

    if (typeof window.finalizarRegistro === "function") {
        window.finalizarRegistro(docValue);
    } else {
        console.error("El script auth-login-v2.js aún no ha expuesto la función finalizarRegistro en 'window'.");
    }
};

// Index.cshtml
window.showIndexToast = function(isError, title, text) {
    if (typeof Swal !== 'undefined') {
        Swal.fire({
            icon: isError ? 'error' : 'success',
            title: title,
            text: text,
            timer: 3000,
            showConfirmButton: false,
            borderRadius: '1.5rem',
            customClass: {
                popup: 'rounded-3xl shadow-xl'
            }
        });
    }
};

// SaberMas.cshtml
window.initSaberMasForm = function() {
    const form = document.getElementById("contact-form");
    const toast = document.getElementById("toast-success");
    const btnSubmit = document.getElementById("btn-submit");

    if (!form) return;

    async function handleSubmit(event) {
        event.preventDefault();
        
        btnSubmit.disabled = true;
        btnSubmit.innerText = "Enviando...";

        const data = new FormData(event.target);
        
        try {
            const response = await fetch("https://formspree.io/f/xrejpdrz", {
                method: "POST",
                body: data,
                headers: {
                    'Accept': 'application/json'
                }
            });

            if (response.ok) {
                window.showSaberMasToast();
                form.reset();
            } else {
                alert("Error al enviar. Por favor verifica los datos.");
            }
        } catch (error) {
            alert("Hubo un problema de conexión.");
        } finally {
            btnSubmit.disabled = false;
            btnSubmit.innerText = "Enviar Solicitud";
        }
    }

    // Only add listener if it hasn't been added yet to avoid duplicates
    if (!form.dataset.listenerAdded) {
        form.addEventListener("submit", handleSubmit);
        form.dataset.listenerAdded = 'true';
    }

    window.showSaberMasToast = function() {
        if (!toast) return;
        toast.classList.remove('hidden');
        toast.classList.remove('opacity-0', 'translate-y-[-20px]');
        
        setTimeout(() => {
            window.closeSaberMasToast();
        }, 5000);
    };

    window.closeSaberMasToast = function() {
        if (!toast) return;
        toast.classList.add('opacity-0', 'translate-y-[-20px]');
        setTimeout(() => {
            toast.classList.add('hidden');
        }, 300);
    };
};

// _Layout.cshtml
window.initLayoutScripts = function() {
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }

    const btnToggle = document.getElementById('btn-toggle-sidebar');
    const sidebar = document.getElementById('sidebar');
    const backdrop = document.getElementById('sidebar-backdrop');
    const iconMenu = document.getElementById('icon-menu');
    const iconClose = document.getElementById('icon-close');

    if (btnToggle && sidebar && backdrop) {
        function toggleSidebar() {
            const isOpen = !sidebar.classList.contains('-translate-x-full');

            if (isOpen) {
                sidebar.classList.add('-translate-x-full');
                backdrop.classList.add('hidden');
                if (iconMenu && iconClose) {
                    iconMenu.classList.remove('hidden');
                    iconClose.classList.add('hidden');
                }
            } else {
                sidebar.classList.remove('-translate-x-full');
                backdrop.classList.remove('hidden');
                if (iconMenu && iconClose) {
                    iconMenu.classList.add('hidden');
                    iconClose.classList.remove('hidden');
                }
            }
        }

        // Avoid multiple listeners if called multiple times
        if (!btnToggle.dataset.listenerAdded) {
            btnToggle.addEventListener('click', toggleSidebar);
            backdrop.addEventListener('click', toggleSidebar);
            btnToggle.dataset.listenerAdded = 'true';
        }
    }

    const btnLogout = document.getElementById('btn-logout');
    const logoutForm = document.getElementById('logout-form');

    if (btnLogout && logoutForm && !btnLogout.dataset.listenerAdded) {
        btnLogout.addEventListener('click', function () {
            if (typeof Swal !== 'undefined') {
                Swal.fire({
                    title: '¿Estás seguro de que quieres salir?',
                    text: "Se cerrará tu sesión actual en el sistema.",
                    icon: 'warning',
                    showCancelButton: true,
                    confirmButtonColor: '#4f46e5',
                    cancelButtonColor: '#e11d48',
                    confirmButtonText: 'Sí, cerrar sesión',
                    cancelButtonText: 'Cancelar',
                    background: '#0f172a',
                    color: '#f8fafc',
                    heightAuto: false,
                    customClass: {
                        popup: 'rounded-2xl border border-slate-800 shadow-2xl',
                        confirmButton: 'rounded-xl font-semibold',
                        cancelButton: 'rounded-xl font-semibold'
                    }
                }).then((result) => {
                    if (result.isConfirmed) {
                        Swal.fire({
                            title: 'Cerrando sesión...',
                            text: 'Por favor espera un momento',
                            allowOutsideClick: false,
                            background: '#0f172a',
                            color: '#f8fafc',
                            showConfirmButton: false,
                            heightAuto: false,
                            customClass: {
                                popup: 'rounded-2xl border border-slate-800 shadow-xl'
                            },
                            didOpen: () => {
                                Swal.showLoading();
                            }
                        });
                        logoutForm.submit();
                    }
                });
            } else {
                logoutForm.submit();
            }
        });
        btnLogout.dataset.listenerAdded = 'true';
    }
};

document.addEventListener('DOMContentLoaded', function() {
    initLayoutScripts();
    initSaberMasForm();
});
