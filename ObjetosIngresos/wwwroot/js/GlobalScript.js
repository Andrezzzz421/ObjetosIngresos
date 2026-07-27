// ==========================================
// 1. PERFIL & AUTENTICACIÓN
// ==========================================
window.hideToast = function () {
    const toast = document.getElementById("toastError");
    if (toast) {
        toast.classList.add("opacity-0", "translate-y-10");
        setTimeout(() => toast.classList.add("hidden"), 300);
    }
};

window.confirmarCambio = function () {
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

window.togglePassword = function () {
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

window.despacharRegistro = function () {
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

// ==========================================
// 2. VISTAS GENERALES (INDEX & SABER MÁS)
// ==========================================
window.showIndexToast = function (isError, title, text) {
    if (typeof Swal !== 'undefined') {
        Swal.fire({
            icon: isError ? 'error' : 'success',
            title: title,
            text: text,
            timer: 3000,
            showConfirmButton: false,
            customClass: {
                popup: 'rounded-3xl shadow-xl'
            }
        });
    }
};

window.initSaberMasForm = function () {
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
                headers: { 'Accept': 'application/json' }
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

    if (!form.dataset.listenerAdded) {
        form.addEventListener("submit", handleSubmit);
        form.dataset.listenerAdded = 'true';
    }

    window.showSaberMasToast = function () {
        if (!toast) return;
        toast.classList.remove('hidden', 'opacity-0', 'translate-y-[-20px]');
        setTimeout(() => { window.closeSaberMasToast(); }, 5000);
    };

    window.closeSaberMasToast = function () {
        if (!toast) return;
        toast.classList.add('opacity-0', 'translate-y-[-20px]');
        setTimeout(() => { toast.classList.add('hidden'); }, 300);
    };
};

// ==========================================
// 3. LAYOUT & MENÚS NAVIGATION
// ==========================================
window.initLayoutScripts = function () {
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
                            customClass: { popup: 'rounded-2xl border border-slate-800 shadow-xl' },
                            didOpen: () => { Swal.showLoading(); }
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

// ==========================================
// 4. OPERACIONES DE CATÁLOGOS (AJAX)
// ==========================================
function ejecutarEliminacionAjax(url, id, elementoFilaId) {
    $.ajax({
        url: url,
        type: 'POST',
        data: { id: id },
        success: function (response) {
            if (response.success) {
                let fila = document.getElementById(elementoFilaId);
                if (fila) {
                    fila.style.transition = 'opacity 0.3s ease';
                    fila.style.opacity = '0';
                    setTimeout(() => fila.remove(), 300);
                }
            } else {
                alert(response.message);
            }
        },
        error: function () {
            alert("Ocurrió un error de comunicación con el servidor.");
        }
    });
}

function eliminarRegistro(id) {
    if (confirm("¿Estás seguro de que deseas eliminar esta marca?")) {
        ejecutarEliminacionAjax('@Url.Action("DeleteMarca", "Catalogos")', id, 'fila-marca-' + id);
    }
}

function eliminarRegional(id) {
    if (confirm("¿Estás seguro de que deseas eliminar esta regional? Nota: Si tiene centros de formación asociados, la operación podría fallar.")) {
        ejecutarEliminacionAjax('@Url.Action("DeleteRegional", "Catalogos")', id, 'fila-regional-' + id);
    }
}

function eliminarSede(id) {
    if (confirm("¿Estás seguro de que deseas eliminar esta sede física del sistema?")) {
        ejecutarEliminacionAjax('@Url.Action("DeleteSede", "Catalogos")', id, 'fila-sede-' + id);
    }
}

function eliminarTipoDetalle(id) {
    if (confirm("¿Estás seguro de que deseas eliminar este tipo de detalle?")) {
        ejecutarEliminacionAjax('@Url.Action("DeleteTipoDetalle", "Catalogos")', id, 'fila-tipo-' + id);
    }
}

function eliminarElemento(id) {
    if (confirm("¿Está seguro de que desea remover este equipo y todos sus accesorios en cascada?")) {
        ejecutarEliminacionAjax('@Url.Action("Delete", "Elementos")', id, 'fila-elemento-' + id);
    }
}

// ==========================================
// 5. GESTIÓN INLINE DE ROL / TIPO USUARIO
// ==========================================
function habilitarEdicion(id) {
    document.getElementById('txt-rol-' + id).classList.add('hidden');
    document.getElementById('input-rol-' + id).classList.remove('hidden');
    document.getElementById('actions-read-' + id).classList.add('hidden');
    document.getElementById('actions-edit-' + id).classList.remove('hidden');
    document.getElementById('input-rol-' + id).focus();
}

function cancelarEdicion(id) {
    const input = document.getElementById('input-rol-' + id);
    const spanText = document.getElementById('txt-rol-' + id);
    input.value = spanText.innerText.trim();

    input.classList.add('hidden');
    spanText.classList.remove('hidden');
    document.getElementById('actions-edit-' + id).classList.add('hidden');
    document.getElementById('actions-read-' + id).classList.remove('hidden');
}

function guardarEdicion(id) {
    const nuevoNombre = document.getElementById('input-rol-' + id).value;

    $.ajax({
        url: '@Url.Action("UpdateTipoUsuario", "Catalogos")',
        type: 'POST',
        data: { IdTipoUsuario: id, NombreTipo: nuevoNombre },
        success: function (response) {
            if (response.success) {
                document.getElementById('txt-rol-' + id).innerText = nuevoNombre;
                cancelarEdicion(id);
            } else {
                alert(response.message);
            }
        },
        error: function () {
            alert("Error de red al intentar actualizar el rol.");
        }
    });
}

function eliminarRol(id) {
    if (confirm("¿Está seguro de que desea remover este rol del sistema?")) {
        ejecutarEliminacionAjax('@Url.Action("DeleteTipoUsuario", "Catalogos")', id, 'fila-rol-' + id);
    }
}

// ==========================================
// 6. FORMULARIOS DE ELEMENTOS (CREATE & EDIT)
// ==========================================
var globalContadorFilas = 1;

document.addEventListener("DOMContentLoaded", function () {
    const contenedor = document.getElementById('contenedor-detalles');
    if (contenedor && contenedor.getAttribute('data-index')) {
        globalContadorFilas = parseInt(contenedor.getAttribute('data-index')) || 1;
    }
});

function agregarFilaDetalle() {
    const contenedor = document.getElementById('contenedor-detalles');
    if (!contenedor) return;

    const plantilla = document.getElementById('plantilla-detalle');

    if (plantilla) {
        const clon = plantilla.content.cloneNode(true);
        const select = clon.querySelector('select');
        const input = clon.querySelector('input');

        if (select) select.name = `detalles[${globalContadorFilas}].IdTipoDetalle`;
        if (input) input.name = `detalles[${globalContadorFilas}].Nota`;

        contenedor.appendChild(clon);
    } else {
        const nuevaFila = document.createElement('div');
        nuevaFila.className = "grid grid-cols-1 md:grid-cols-12 gap-3 p-4 bg-slate-50 border border-slate-200 rounded-xl relative group fila-detalle";
        nuevaFila.innerHTML = `
        <div class="md:col-span-4">
            <label class="block text-[10px] font-bold uppercase tracking-wider text-slate-400 mb-1">Tipo de Detalle</label>
            <select name="detalles[${globalContadorFilas}].IdTipoDetalle" required class="w-full rounded-lg border border-slate-300 bg-white px-3 py-1.5 text-xs text-slate-900 focus:outline-none focus:border-indigo-500">
                <option value="">-- Seleccione --</option>
                @if (ViewBag.TiposDetalle != null) {
                    foreach (var tipo in ViewBag.TiposDetalle) {
                        <option value="@tipo.IdTipoDetalle">@tipo.Nombre</option>
                    }
                }
            </select>
        </div>
        <div class="md:col-span-7">
            <label class="block text-[10px] font-bold uppercase tracking-wider text-slate-400 mb-1">Notas / Especificaciones</label>
            <input type="text" name="detalles[${globalContadorFilas}].Nota" placeholder="Ej. Especificaciones adicionales" class="w-full rounded-lg border border-slate-300 bg-white px-3 py-1.5 text-xs text-slate-900 focus:outline-none focus:border-indigo-500" />
        </div>
        <div class="md:col-span-1 flex items-end justify-center">
            <button type="button" onclick="removerFila(this)" class="mb-1 rounded-lg p-1.5 text-slate-400 hover:bg-red-50 hover:text-red-600 transition-all opacity-100 md:opacity-0 group-hover:opacity-100">
                &times;
            </button>
        </div>`;
        contenedor.appendChild(nuevaFila);
    }

    globalContadorFilas++;
    contenedor.setAttribute('data-index', globalContadorFilas);
}

function removerFila(btn) {
    const contenedor = document.getElementById('contenedor-detalles');
    const filas = document.querySelectorAll('.fila-detalle');

    if (filas.length > 1) {
        btn.closest('.fila-detalle').remove();
        reordenarIndices();
    } else {
        alert("El equipo debe conservar al menos un espacio para detalles o accesorios.");
    }
}

function reordenarIndices() {
    const contenedor = document.getElementById('contenedor-detalles');
    const filas = document.querySelectorAll('.fila-detalle');
    globalContadorFilas = 0;

    filas.forEach((fila) => {
        const select = fila.querySelector('select');
        const input = fila.querySelector('input');

        if (select) select.name = `detalles[${globalContadorFilas}].IdTipoDetalle`;
        if (input) input.name = `detalles[${globalContadorFilas}].Nota`;

        globalContadorFilas++;
    });

    if (contenedor) contenedor.setAttribute('data-index', globalContadorFilas);
}

function previewImage(input) {
    const file = input.files[0];
    if (file) {
        const reader = new FileReader();
        reader.onload = function (e) {
            const imgPreview = document.getElementById('preview');
            if (imgPreview) {
                imgPreview.src = e.target.result;
                imgPreview.classList.remove('hidden');
            }
            const textPlaceholder = document.getElementById('upload-placeholder');
            if (textPlaceholder) {
                textPlaceholder.textContent = "Nueva foto cargada con éxito";
                textPlaceholder.classList.add('hidden');
            }
        }
        reader.readAsDataURL(file);
    }
}

// ==========================================
// 7. LISTENERS GENERALES DOM
// ==========================================
document.addEventListener('DOMContentLoaded', function () {
    initLayoutScripts();
    initSaberMasForm();

    const btnToggle = document.getElementById("btn-toggle-catalogos");
    const menuCatalogos = document.getElementById("menu-catalogos");
    const iconChevron = document.getElementById("icon-chevron-catalogos");

    if (btnToggle && menuCatalogos && iconChevron) {
        btnToggle.addEventListener("click", function () {
            menuCatalogos.classList.toggle("hidden");
            iconChevron.classList.toggle("rotate-180");
        });
    }
});

// ==========================================
// 8. CAPTURA Y NOTIFICACIÓN DE ERRORES DESDE EL BACKEND
// ==========================================
document.addEventListener("DOMContentLoaded", function () {
    const carrier = document.getElementById("validation-errors-carrier");

    if (carrier) {
        const rawErrors = carrier.getAttribute("data-errors");

        if (rawErrors) {
            try {
                const listaErrores = JSON.parse(rawErrors);

                if (listaErrores && listaErrores.length > 0) {
                    let mensajeHtml = '<ul class="text-left text-sm space-y-1.5 mt-2">';
                    listaErrores.forEach(function (error) {
                        mensajeHtml += `<li class="flex items-start gap-2 text-slate-600"><span class="text-red-500 mt-0.5">•</span> <span>${error}</span></li>`;
                    });
                    mensajeHtml += '</ul>';

                    if (typeof Swal !== 'undefined') {
                        Swal.fire({
                            icon: 'error',
                            title: 'Revisa los campos requeridos',
                            html: mensajeHtml,
                            confirmButtonColor: '#4f46e5',
                            customClass: {
                                popup: 'rounded-3xl border border-slate-100 shadow-xl p-6'
                            }
                        });
                    }
                }
            } catch (e) {
                console.error("Error al procesar los errores del ModelState:", e);
            }
        }
    }
});


// ==========================================
// 9. Validaciones
// ==========================================

document.addEventListener("DOMContentLoaded", function () {
    inicializarControlesEstrictos();
});

function inicializarControlesEstrictos() {
    document.querySelectorAll('.input-solo-letras').forEach(function (input) {
        input.addEventListener('keypress', function (e) {
            const regex = /^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ\s]$/;
            if (!regex.test(e.key)) {
                e.preventDefault();
                mostrarAdvertencia(input, "Solo se permiten letras en este campo");
            }
        });

        input.addEventListener('input', function () {
            const valorOriginal = this.value;
            const valorLimpio = this.value.replace(/[^a-zA-ZáéíóúÁÉÍÓÚñÑüÜ\s]/g, '');
            if (valorOriginal !== valorLimpio) {
                this.value = valorLimpio;
                mostrarAdvertencia(input, "Se removieron los caracteres no permitidos");
            }
        });
    });

    document.querySelectorAll('.input-solo-numeros').forEach(function (input) {
        input.addEventListener('keypress', function (e) {
            const regex = /^[0-9]$/;
            if (!regex.test(e.key)) {
                e.preventDefault();
                mostrarAdvertencia(input, "Solo se permiten números en este campo");
            }
        });

        input.addEventListener('input', function () {
            const valorOriginal = this.value;
            const valorLimpio = this.value.replace(/[^0-9]/g, '');
            if (valorOriginal !== valorLimpio) {
                this.value = valorLimpio;
                mostrarAdvertencia(input, "Se removieron las letras o caracteres especiales");
            }
        });
    });

    document.querySelectorAll('form').forEach(function (form) {
        form.addEventListener('submit', function (e) {
            let formularioValido = true;

            form.querySelectorAll('select[required]').forEach(function (select) {
                if (select.value === "") {
                    formularioValido = false;
                    e.preventDefault();

                    mostrarAdvertencia(select, "Debes seleccionar una opción válida");
                }
            });
        });
    });
}

function mostrarAdvertencia(input, mensaje) {
    if (input.parentNode.querySelector('.js-warning-bubble')) return;

    if (!input.parentNode.classList.contains('relative')) {
        input.parentNode.classList.add('relative');
    }

    const warning = document.createElement('div');
    warning.className = "js-warning-bubble absolute z-50 left-1 -bottom-7 bg-amber-500 text-white text-[10px] font-bold px-2 py-1 rounded-md shadow-md transition-all duration-300 opacity-0 transform translate-y-[-5px]";
    warning.innerText = mensaje;

    input.parentNode.appendChild(warning);

    const clasesOriginales = ["border-slate-200", "focus:ring-indigo-500"];
    const clasesError = ["border-amber-500", "focus:ring-amber-500", "ring-2", "ring-amber-200"];

    input.classList.remove(...clasesOriginales);
    input.classList.add(...clasesError);

    setTimeout(() => {
        warning.classList.remove('opacity-0', 'translate-y-[-5px]');
        warning.classList.add('opacity-100', 'translate-y-0');
    }, 10);

    setTimeout(() => {
        warning.classList.remove('opacity-100', 'translate-y-0');
        warning.classList.add('opacity-0', 'translate-y-[-5px]');

        setTimeout(() => {
            warning.remove();
            input.classList.remove(...clasesError);
            input.classList.add(...clasesOriginales);
        }, 300);
    }, 2000);
}