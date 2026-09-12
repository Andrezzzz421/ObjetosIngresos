
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
async function confirmarYEliminarCatalogo(url, id, elementoFilaId, tipoItem) {
    if (typeof Swal === 'undefined') {
        if (!confirm(`¿Estás seguro de que deseas eliminar ${tipoItem}?`)) return;
    } else {
        const result = await Swal.fire({
            title: `¿Eliminar ${tipoItem}?`,
            text: `Esta acción no se puede deshacer. ¿Deseas continuar?`,
            icon: 'warning',
            iconColor: '#ef4444',
            showCancelButton: true,
            confirmButtonText: 'Sí, eliminar',
            cancelButtonText: 'Cancelar',
            confirmButtonColor: '#ef4444',
            cancelButtonColor: '#6b7280',
            customClass: {
                popup: 'rounded-2xl',
                confirmButton: 'rounded-xl font-semibold px-5 py-2.5',
                cancelButton: 'rounded-xl font-semibold px-5 py-2.5'
            }
        });

        if (!result.isConfirmed) return;
    }

    try {
        const formData = new FormData();
        formData.append('id', id);

        const resp = await fetch(url, {
            method: 'POST',
            body: formData
        });

        const data = await resp.json();
        if (data.success) {
            const fila = document.getElementById(elementoFilaId);
            if (fila) {
                fila.style.transition = 'opacity 0.3s ease';
                fila.style.opacity = '0';
                setTimeout(() => fila.remove(), 300);
            }
            if (typeof Swal !== 'undefined') {
                Swal.fire({
                    toast: true,
                    position: 'top-end',
                    icon: 'success',
                    title: data.message || `${tipoItem} eliminado correctamente`,
                    showConfirmButton: false,
                    timer: 3000
                });
            }
        } else {
            if (typeof Swal !== 'undefined') {
                Swal.fire({
                    title: 'No se puede eliminar',
                    text: data.message || 'El elemento está en uso o tiene registros dependientes.',
                    icon: 'error',
                    confirmButtonColor: '#4f46e5',
                    customClass: { popup: 'rounded-2xl', confirmButton: 'rounded-xl px-5 py-2.5' }
                });
            } else {
                alert(data.message || 'No se puede eliminar el elemento.');
            }
        }
    } catch (err) {
        if (typeof Swal !== 'undefined') {
            Swal.fire({
                title: 'Error de comunicación',
                text: 'Ocurrió un error al intentar comunicarse con el servidor.',
                icon: 'error',
                confirmButtonColor: '#4f46e5',
                customClass: { popup: 'rounded-2xl', confirmButton: 'rounded-xl px-5 py-2.5' }
            });
        } else {
            alert('Error de comunicación con el servidor.');
        }
    }
}

function eliminarRegistro(id) {
    confirmarYEliminarCatalogo('/Catalogos/DeleteMarca', id, 'fila-marca-' + id, 'esta marca');
}

function eliminarRegional(id) {
    confirmarYEliminarCatalogo('/Catalogos/DeleteRegional', id, 'fila-regional-' + id, 'esta regional');
}

function eliminarCentroFormacion(id) {
    confirmarYEliminarCatalogo('/Catalogos/DeleteCentroFormacion', id, 'fila-centro-' + id, 'este centro de formación');
}

function eliminarSede(id) {
    confirmarYEliminarCatalogo('/Catalogos/DeleteSede', id, 'fila-sede-' + id, 'esta sede');
}

function eliminarTipoDetalle(id) {
    confirmarYEliminarCatalogo('/Catalogos/DeleteTipoDetalle', id, 'fila-tipo-' + id, 'este tipo de detalle');
}

async function eliminarElemento(id, tipoElemento) {
    const nombre = tipoElemento ? `"${tipoElemento}"` : 'este equipo';
    const result = await Swal.fire({
        title: '¿Eliminar equipo?',
        html: `Estás a punto de remover <strong>${nombre}</strong> y todos sus accesorios vinculados. Esta acción no se puede deshacer.`,
        icon: 'warning',
        iconColor: '#ef4444',
        showCancelButton: true,
        confirmButtonText: 'Sí, eliminar',
        cancelButtonText: 'Cancelar',
        confirmButtonColor: '#ef4444',
        cancelButtonColor: '#6b7280',
        customClass: {
            popup: 'rounded-2xl',
            confirmButton: 'rounded-xl font-semibold px-5 py-2.5',
            cancelButton: 'rounded-xl font-semibold px-5 py-2.5'
        }
    });

    if (!result.isConfirmed) return;

    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    const tokenVal = tokenInput ? tokenInput.value : '';

    try {
        const resp = await fetch(`/Elementos/Delete?id=${id}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded'
            },
            body: `id=${id}&__RequestVerificationToken=${encodeURIComponent(tokenVal)}`
        });

        const json = await resp.json();
        if (json.success) {
            const fila = document.getElementById(`fila-elemento-${id}`);
            if (fila) {
                fila.style.transition = 'opacity 0.3s ease';
                fila.style.opacity = '0';
                setTimeout(() => fila.remove(), 300);
            }
            Swal.fire({
                title: '¡Eliminado!',
                text: 'El equipo ha sido eliminado correctamente del inventario.',
                icon: 'success',
                confirmButtonColor: '#4f46e5',
                customClass: { popup: 'rounded-2xl', confirmButton: 'rounded-xl font-semibold' }
            });
        } else {
            Swal.fire({
                title: 'No se puede eliminar',
                text: json.message || 'No se pudo eliminar el equipo.',
                icon: 'error',
                confirmButtonColor: '#ef4444',
                customClass: { popup: 'rounded-2xl', confirmButton: 'rounded-xl font-semibold' }
            });
        }
    } catch (error) {
        Swal.fire({
            title: 'Error de red',
            text: 'No se pudo comunicar con el servidor.',
            icon: 'error',
            confirmButtonColor: '#ef4444',
            customClass: { popup: 'rounded-2xl', confirmButton: 'rounded-xl font-semibold' }
        });
    }
}

function confirmarEdicionElemento() {
    const form = document.getElementById('form-editar-elemento');
    if (!form) return;
    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }

    Swal.fire({
        title: '¿Guardar cambios?',
        text: '¿Deseas actualizar la información de este equipo en el inventario?',
        icon: 'question',
        showCancelButton: true,
        confirmButtonText: 'Sí, guardar',
        cancelButtonText: 'Cancelar',
        confirmButtonColor: '#4f46e5',
        cancelButtonColor: '#6b7280',
        customClass: {
            popup: 'rounded-2xl',
            confirmButton: 'rounded-xl font-semibold px-5 py-2.5',
            cancelButton: 'rounded-xl font-semibold px-5 py-2.5'
        }
    }).then((result) => {
        if (result.isConfirmed) {
            form.submit();
        }
    });
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

async function guardarEdicion(id) {
    const input = document.getElementById('input-rol-' + id);
    const nuevoNombre = input ? input.value : '';

    if (!nuevoNombre || !nuevoNombre.trim()) {
        if (typeof Swal !== 'undefined') {
            Swal.fire({
                icon: 'warning',
                title: 'Campo requerido',
                text: 'El nombre del rol no puede estar vacío.',
                confirmButtonColor: '#4f46e5'
            });
        } else {
            alert('El nombre del rol no puede estar vacío.');
        }
        return;
    }

    try {
        const formData = new FormData();
        formData.append('IdTipoUsuario', id);
        formData.append('NombreTipo', nuevoNombre.trim());

        const resp = await fetch('/Catalogos/UpdateTipoUsuario', {
            method: 'POST',
            body: formData
        });

        const response = await resp.json();
        if (response.success) {
            const spanText = document.getElementById('txt-rol-' + id);
            if (spanText) spanText.innerText = nuevoNombre.trim();
            cancelarEdicion(id);
            if (typeof Swal !== 'undefined') {
                Swal.fire({
                    toast: true,
                    position: 'top-end',
                    icon: 'success',
                    title: 'Rol actualizado con éxito',
                    showConfirmButton: false,
                    timer: 2500
                });
            }
        } else {
            if (typeof Swal !== 'undefined') {
                Swal.fire({
                    icon: 'error',
                    title: 'No se pudo actualizar',
                    text: response.message || 'Error al actualizar el rol.',
                    confirmButtonColor: '#4f46e5'
                });
            } else {
                alert(response.message || 'Error al actualizar el rol.');
            }
        }
    } catch (e) {
        if (typeof Swal !== 'undefined') {
            Swal.fire({
                icon: 'error',
                title: 'Error de red',
                text: 'Error al intentar actualizar el rol.',
                confirmButtonColor: '#4f46e5'
            });
        } else {
            alert('Error de red al intentar actualizar el rol.');
        }
    }
}

function eliminarRol(id) {
    confirmarYEliminarCatalogo('/Catalogos/DeleteTipoUsuario', id, 'fila-rol-' + id, 'este rol');
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

        if (select) {
            select.name = `detalles[${globalContadorFilas}].IdTipoDetalle`;
            select.selectedIndex = 0;
        }
        if (input) {
            input.name = `detalles[${globalContadorFilas}].Nota`;
            input.value = '';
        }

        contenedor.appendChild(clon);
        globalContadorFilas++;
        contenedor.setAttribute('data-index', globalContadorFilas);
    }
}

function removerFila(btn) {
    const fila = btn.closest('.fila-detalle');
    if (fila) {
        fila.remove();
        reordenarIndices();
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

document.addEventListener("DOMContentLoaded", function () {
    const carrier = document.getElementById("validation-errors-carrier");

    if (carrier) {
        let rawErrors = carrier.getAttribute("data-errors");

        if (rawErrors && typeof rawErrors === 'string' && rawErrors.trim() !== "") {
            try {
                if (rawErrors.includes("&quot;")) {
                    rawErrors = rawErrors.replace(/&quot;/g, '"');
                }
                const listaErrores = JSON.parse(rawErrors);

                if (Array.isArray(listaErrores) && listaErrores.length > 0) {
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

document.addEventListener("DOMContentLoaded", function () {
    const camposNumericos = document.querySelectorAll('input[type="number"], .input-solo-numeros');

    camposNumericos.forEach(function (input) {
        input.addEventListener("keypress", function (e) {
            if (e.key < '0' || e.key > '9') {
                e.preventDefault();
            }
        });

        input.addEventListener("paste", function (e) {
            const clipboardData = e.clipboardData || window.clipboardData;
            const dataPegada = clipboardData.getData('Text');

            if (!/^\d+$/.test(dataPegada)) {
                e.preventDefault();
            }
        });
    });
});


//RegistrarEquipo

// Preview de foto al seleccionar
document.getElementById('foto')?.addEventListener('change', function () {
    // Código anterior de preview comentado
});

// Deshabilitar botón al enviar para evitar doble submit
document.getElementById('form-registro')?.addEventListener('submit', function () {
    const btn = document.getElementById('btn-submit');
    if (btn) {
        btn.disabled = true;
        btn.innerHTML = `<svg class="w-5 h-5 animate-spin" fill="none" viewBox="0 0 24 24"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8z"></path></svg> Guardando...`;
    }
});



//Mi Panel/Index

async function confirmarEliminar(id, nombre) {
    const result = await Swal.fire({
        title: '¿Eliminar equipo?',
        html: `Estás a punto de eliminar <strong>${nombre}</strong> de tu panel. Esta acción no se puede deshacer.`,
        icon: 'warning',
        iconColor: '#ef4444',
        showCancelButton: true,
        confirmButtonText: 'Sí, eliminar',
        cancelButtonText: 'Cancelar',
        confirmButtonColor: '#ef4444',
        cancelButtonColor: '#6b7280',
        customClass: {
            popup: 'rounded-2xl',
            confirmButton: 'rounded-xl font-semibold px-5 py-2.5',
            cancelButton: 'rounded-xl font-semibold px-5 py-2.5'
        }
    });

    if (!result.isConfirmed) return;

    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    const resp = await fetch('/MiPanel/EliminarEquipo', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: `id=${id}&__RequestVerificationToken=${encodeURIComponent(token)}`
    });

    const json = await resp.json();
    if (json.success) {
        const card = document.getElementById(`card-equipo-${id}`);
        card?.remove();
        await Swal.fire({
            title: '¡Eliminado!',
            text: 'El equipo ha sido eliminado correctamente.',
            icon: 'success',
            confirmButtonColor: '#4f46e5',
            customClass: { popup: 'rounded-2xl', confirmButton: 'rounded-xl font-semibold' }
        });
        // Recargar para actualizar el indicador de cupos
        location.reload();
    } else {
        Swal.fire({
            title: 'No se pudo eliminar',
            text: json.message,
            icon: 'error',
            confirmButtonColor: '#ef4444',
            customClass: { popup: 'rounded-2xl', confirmButton: 'rounded-xl font-semibold' }
        });
    }
}

// Auto-ocultar alertas después de 5 segundos
setTimeout(() => {
    ['alert-exito', 'alert-error'].forEach(id => {
        const el = document.getElementById(id);
        if (el) el.style.opacity = '0', el.style.transition = 'opacity 0.5s', setTimeout(() => el.remove(), 500);
    });
}, 5000);

//Movimiento/Index

window.token = window.token || (() => document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '');
var token = window.token;

// ──────── Búsqueda ────────
async function buscar() {
    const input = document.getElementById('input-busqueda');
    if (!input) return;
    const query = input.value.trim();

    mostrarEstado('loading');

    try {
        const resp = await fetch(`/Movimiento/Buscar?query=${encodeURIComponent(query)}`);
        const json = await resp.json();

        if (!json.success || !json.data?.length) {
            mostrarEstado('sin-resultados');
            return;
        }

        renderResultados(json.data);
        mostrarEstado('resultados');
    } catch {
        mostrarEstado('sin-resultados');
    }
}

document.getElementById('btn-buscar')?.addEventListener('click', buscar);
document.getElementById('input-busqueda')?.addEventListener('keydown', e => { if (e.key === 'Enter') buscar(); });

function renderResultados(elementos) {
    const contenedor = document.getElementById('contenedor-resultados');
    contenedor.innerHTML = '';

    elementos.forEach(el => {
        const fotoHtml = el.foto
            ? `<img src="${el.foto}" alt="foto" class="w-full h-full object-cover cursor-pointer hover:opacity-90 transition-opacity" onclick="abrirModalFoto('${el.foto}')" />`
            : `<div class="w-full h-full flex items-center justify-center text-slate-400 bg-slate-100">
                       <svg class="w-10 h-10" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z"/></svg>
                   </div>`;

        const estadoBadge = el.tieneMovimientoActivo
            ? `<span class="inline-flex items-center gap-1.5 bg-emerald-100 text-emerald-700 text-xs font-bold px-2.5 py-1 rounded-full">
                       <span class="w-1.5 h-1.5 bg-emerald-500 rounded-full animate-pulse"></span> INGRESADO
                   </span>`
            : `<span class="inline-flex items-center gap-1.5 bg-slate-100 text-slate-500 text-xs font-bold px-2.5 py-1 rounded-full">
                       <span class="w-1.5 h-1.5 bg-slate-400 rounded-full"></span> FUERA
                   </span>`;

        const accionHtml = el.tieneMovimientoActivo
            ? `<div class="space-y-2">
                       <div class="flex items-center gap-2 text-xs text-slate-500 bg-slate-50 rounded-lg px-3 py-2">
                           <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"/></svg>
                           Entrada: <strong>${el.fechaEntrada}</strong>
                       </div>
                       <button onclick="hacerCheckOut(${el.idMovimientoActivo}, this)"
                               class="w-full flex items-center justify-center gap-2 bg-rose-600 text-white font-semibold py-2.5 rounded-xl hover:bg-rose-500 transition-colors text-sm shadow-md shadow-rose-600/20">
                           <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1"/></svg>
                           Registrar Salida (Check-Out)
                       </button>
                   </div>`
            : `<button onclick="hacerCheckIn(${el.idElemento}, this)"
                           class="w-full flex items-center justify-center gap-2 bg-indigo-600 text-white font-semibold py-2.5 rounded-xl hover:bg-indigo-500 transition-colors text-sm shadow-md shadow-indigo-600/20">
                       <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 16l-4-4m0 0l4-4m-4 4h14m-5 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h7a3 3 0 013 3v1"/></svg>
                       Registrar Entrada (Check-In)
                   </button>`;

        const card = document.createElement('div');
        card.id = `card-resultado-${el.idElemento}`;
        card.className = 'bg-white border border-slate-200 rounded-2xl overflow-hidden shadow-sm hover:shadow-md transition-shadow';
        card.innerHTML = `
                <div class="flex flex-col sm:flex-row">
                    <!-- Foto -->
                    <div class="sm:w-40 h-40 sm:h-auto shrink-0 overflow-hidden bg-slate-100">
                        ${fotoHtml}
                    </div>
                    <!-- Info -->
                    <div class="flex-grow p-5 flex flex-col justify-between gap-4">
                        <div class="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-3">
                            <div>
                                <div class="flex items-center gap-2 mb-1">
                                    <h3 class="font-bold text-slate-900">${el.tipoElemento}</h3>
                                    ${estadoBadge}
                                </div>
                                <p class="text-sm text-slate-500">${el.marca}</p>
                                <p class="text-xs font-mono text-slate-400 mt-1">Serial: ${el.serial}</p>
                            </div>
                            <div class="text-right shrink-0">
                                <p class="font-semibold text-slate-800 text-sm">${el.propietario}</p>
                                <p class="text-xs text-slate-400 font-mono">Doc: ${el.documento}</p>
                            </div>
                        </div>
                        <div id="accion-${el.idElemento}">
                            ${accionHtml}
                        </div>
                        <button onclick="verHistorial(${el.idElemento}, this)"
                                class="text-xs text-slate-400 hover:text-indigo-600 transition-colors text-left font-medium">
                            Ver historial de ingresos ›
                        </button>
                        <div id="historial-${el.idElemento}" class="hidden mt-2"></div>
                    </div>
                </div>`;

        contenedor.appendChild(card);
    });
}

// ──────── Check-In ────────
async function hacerCheckIn(idElemento, btn) {
    // Preguntar por la sede
    const selectSede = document.getElementById('select-sede-global');
    const opcionesSede = {};
    for (const opt of selectSede.options) {
        if (opt.value) opcionesSede[opt.value] = opt.text;
    }

    const { value: idSede, isConfirmed } = await Swal.fire({
        title: 'Seleccionar Sede',
        html: `<select id="swal-sede" class="swal2-input" style="width:100%">
                       <option value="">— Seleccione sede —</option>
                       ${Object.entries(opcionesSede).map(([v, t]) => `<option value="${v}">${t}</option>`).join('')}
                   </select>`,
        showCancelButton: true,
        confirmButtonText: 'Confirmar Check-In',
        cancelButtonText: 'Cancelar',
        confirmButtonColor: '#4f46e5',
        cancelButtonColor: '#6b7280',
        customClass: { popup: 'rounded-2xl', confirmButton: 'rounded-xl font-semibold', cancelButton: 'rounded-xl font-semibold' },
        preConfirm: () => {
            const v = document.getElementById('swal-sede').value;
            if (!v) { Swal.showValidationMessage('Debes seleccionar una sede'); }
            return v;
        }
    });

    if (!isConfirmed || !idSede) return;

    btn.disabled = true;
    btn.textContent = 'Registrando...';

    try {
        const resp = await fetch('/Movimiento/CheckIn', {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: `idElemento=${idElemento}&idSede=${idSede}&__RequestVerificationToken=${encodeURIComponent(token())}`
        });
        const json = await resp.json();

        if (json.success) {
            Swal.fire({
                title: '¡Entrada registrada!',
                text: `Fecha: ${json.fechaEntrada}`,
                icon: 'success',
                confirmButtonColor: '#4f46e5',
                customClass: { popup: 'rounded-2xl', confirmButton: 'rounded-xl font-semibold' }
            }).then(() => buscar());
        } else {
            Swal.fire({ title: 'Error', text: json.message, icon: 'error', confirmButtonColor: '#ef4444', customClass: { popup: 'rounded-2xl', confirmButton: 'rounded-xl font-semibold' } });
            btn.disabled = false;
            btn.textContent = 'Registrar Entrada (Check-In)';
        }
    } catch (error) {
        Swal.fire({ title: 'Error de red', text: error.message, icon: 'error', confirmButtonColor: '#ef4444', customClass: { popup: 'rounded-2xl', confirmButton: 'rounded-xl font-semibold' } });
        btn.disabled = false;
        btn.textContent = 'Registrar Entrada (Check-In)';
    }
}

// ──────── Check-Out ────────
async function hacerCheckOut(idMovimiento, btn) {
    const confirm = await Swal.fire({
        title: '¿Registrar salida?',
        text: 'Se registrará la hora exacta de salida del equipo.',
        icon: 'question',
        showCancelButton: true,
        confirmButtonText: 'Sí, registrar salida',
        cancelButtonText: 'Cancelar',
        confirmButtonColor: '#e11d48',
        cancelButtonColor: '#6b7280',
        customClass: { popup: 'rounded-2xl', confirmButton: 'rounded-xl font-semibold', cancelButton: 'rounded-xl font-semibold' }
    });

    if (!confirm.isConfirmed) return;

    btn.disabled = true;
    btn.textContent = 'Registrando...';

    try {
        const resp = await fetch('/Movimiento/CheckOut', {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: `idMovimiento=${idMovimiento}&__RequestVerificationToken=${encodeURIComponent(token())}`
        });
        const json = await resp.json();

        if (json.success) {
            Swal.fire({
                title: '¡Salida registrada!',
                text: `Fecha: ${json.fechaSalida}`,
                icon: 'success',
                confirmButtonColor: '#4f46e5',
                customClass: { popup: 'rounded-2xl', confirmButton: 'rounded-xl font-semibold' }
            }).then(() => buscar());
        } else {
            Swal.fire({ title: 'Error', text: json.message, icon: 'error', confirmButtonColor: '#ef4444', customClass: { popup: 'rounded-2xl', confirmButton: 'rounded-xl font-semibold' } });
            btn.disabled = false;
            btn.textContent = 'Registrar Salida (Check-Out)';
        }
    } catch (error) {
        Swal.fire({ title: 'Error de red', text: error.message, icon: 'error', confirmButtonColor: '#ef4444', customClass: { popup: 'rounded-2xl', confirmButton: 'rounded-xl font-semibold' } });
        btn.disabled = false;
        btn.textContent = 'Registrar Salida (Check-Out)';
    }
}

// ──────── Historial ────────
async function verHistorial(idElemento, btn) {
    const contenedor = document.getElementById(`historial-${idElemento}`);
    if (!contenedor.classList.contains('hidden')) {
        contenedor.classList.add('hidden');
        btn.textContent = 'Ver historial de ingresos ›';
        return;
    }

    btn.textContent = 'Cargando...';
    const resp = await fetch(`/Movimiento/Historial?idElemento=${idElemento}`);
    const json = await resp.json();
    btn.textContent = 'Ocultar historial ‹';

    if (!json.success || !json.data?.length) {
        contenedor.innerHTML = '<p class="text-xs text-slate-400 italic">Sin registros de movimientos.</p>';
    } else {
        const filas = json.data.map(m => `
                <tr class="border-t border-slate-100 text-xs">
                    <td class="py-2 pr-4 text-slate-500">${m.fechaEntrada ?? '—'}</td>
                    <td class="py-2 pr-4 text-slate-500">${m.fechaSalida}</td>
                    <td class="py-2 pr-4 text-slate-500">${m.sede}</td>
                    <td class="py-2">
                        <span class="inline-block px-2 py-0.5 rounded-full text-[10px] font-bold ${m.estado === 'Activo' ? 'bg-emerald-100 text-emerald-700' : 'bg-slate-100 text-slate-500'}">
                            ${m.estado}
                        </span>
                    </td>
                </tr>`).join('');

        contenedor.innerHTML = `
                <div class="overflow-x-auto border border-slate-100 rounded-xl">
                    <table class="w-full text-left">
                        <thead class="bg-slate-50">
                            <tr class="text-[10px] font-bold uppercase text-slate-400 tracking-wider">
                                <th class="px-3 py-2">Entrada</th>
                                <th class="px-3 py-2">Salida</th>
                                <th class="px-3 py-2">Sede</th>
                                <th class="px-3 py-2">Estado</th>
                            </tr>
                        </thead>
                        <tbody class="divide-y divide-slate-50 text-slate-700">
                            ${filas}
                        </tbody>
                    </table>
                </div>`;
    }

    contenedor.classList.remove('hidden');
}

// ──────── Modal de foto ────────
function abrirModalFoto(src) {
    document.getElementById('modal-foto-img').src = src;
    document.getElementById('modal-foto').classList.remove('hidden');
    document.body.style.overflow = 'hidden';
}

function cerrarModalFoto() {
    document.getElementById('modal-foto').classList.add('hidden');
    document.getElementById('modal-foto-img').src = '';
    document.body.style.overflow = '';
}

document.addEventListener('keydown', e => { if (e.key === 'Escape') cerrarModalFoto(); });

// ──────── Helpers de estado ────────
function mostrarEstado(estado) {
    const sec = document.getElementById('seccion-resultados');
    const sin = document.getElementById('estado-sin-resultados');
    const load = document.getElementById('loading');
    if (!sec || !sin || !load) return;

    sec.classList.add('hidden');
    sin.classList.add('hidden');
    load.classList.add('hidden');

    if (estado === 'resultados') sec.classList.remove('hidden');
    else if (estado === 'sin-resultados') sin.classList.remove('hidden');
    else if (estado === 'loading') load.classList.remove('hidden');
}

// Cargar todos los equipos al iniciar si estamos en la vista de Control de Ingresos
function inicializarMovimiento() {
    if (document.getElementById('input-busqueda')) {
        buscar();
    }
}

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', inicializarMovimiento);
} else {
    inicializarMovimiento();
}

document.addEventListener("DOMContentLoaded", function () {
    const selectTipo = document.getElementById("selectTipoElemento");
    const contenedorOtro = document.getElementById("contenedor-otro");
    const inputOtro = document.getElementById("otroTipoElemento");

    if (!selectTipo || !contenedorOtro || !inputOtro) return;

    function gestionarCambioTipo() {
        if (selectTipo.value === "Otro") {
            // Mostrar campo personalizado
            contenedorOtro.classList.remove("hidden");
            inputOtro.required = true;

            // Intercambiar el name al input de texto
            inputOtro.setAttribute("name", "tipoElemento");
            selectTipo.removeAttribute("name");
        } else {
            // Ocultar campo personalizado
            contenedorOtro.classList.add("hidden");
            inputOtro.required = false;

            // Intercambiar el name al select
            selectTipo.setAttribute("name", "tipoElemento");
            inputOtro.removeAttribute("name");
            inputOtro.value = "";
        }
    }

    selectTipo.addEventListener("change", gestionarCambioTipo);
    gestionarCambioTipo(); // Evaluar al cargar por si se restaura el estado del formulario
}); 

// ==========================================
// PANEL ADMINISTRATIVO
// ==========================================
function initAdminPanel() {
    // 1. Animar todas las barras de progreso (sedes y distribución de inventario)
    const barras = document.querySelectorAll('[data-admin-progress]');
    if (barras.length > 0) {
        requestAnimationFrame(() => {
            setTimeout(() => {
                barras.forEach(function (contenedor) {
                    const pct = contenedor.getAttribute('data-admin-progress') || '0';
                    const fill = contenedor.querySelector('div');
                    if (fill) fill.style.width = pct + '%';
                });
            }, 120);
        });
    }

    // 2. Filtro en tiempo real y buscador para la tabla de movimientos
    const searchInput = document.querySelector('[data-admin-search]');
    const filterButtons = document.querySelectorAll('[data-admin-filter]');
    const tableRows = document.querySelectorAll('[data-admin-row]');
    const emptyState = document.querySelector('[data-admin-search-empty]');

    let currentFilter = 'todos';
    let currentSearch = '';

    function aplicarFiltrosMovimientos() {
        if (!tableRows.length) return;

        let visibleCount = 0;
        const query = currentSearch.trim().toLowerCase();

        tableRows.forEach(row => {
            const status = row.getAttribute('data-status') || '';
            const textContent = row.textContent.toLowerCase();

            const matchesStatus = (currentFilter === 'todos') || (status === currentFilter);
            const matchesSearch = !query || textContent.includes(query);

            if (matchesStatus && matchesSearch) {
                row.classList.remove('hidden');
                visibleCount++;
            } else {
                row.classList.add('hidden');
            }
        });

        if (emptyState) {
            if (visibleCount === 0 && tableRows.length > 0) {
                emptyState.classList.remove('hidden');
                emptyState.classList.add('flex');
            } else {
                emptyState.classList.add('hidden');
                emptyState.classList.remove('flex');
            }
        }
    }

    if (searchInput) {
        searchInput.addEventListener('input', function (e) {
            currentSearch = e.target.value;
            aplicarFiltrosMovimientos();
        });
    }

    if (filterButtons.length > 0) {
        filterButtons.forEach(btn => {
            btn.addEventListener('click', function () {
                const targetFilter = this.getAttribute('data-admin-filter');
                if (!targetFilter) return;

                currentFilter = targetFilter;

                // Actualizar estilos activos de botones
                filterButtons.forEach(b => {
                    b.classList.remove('bg-indigo-600', 'text-white', 'shadow-sm', 'font-bold');
                    b.classList.add('text-slate-600', 'font-semibold');
                });
                this.classList.remove('text-slate-600', 'font-semibold');
                this.classList.add('bg-indigo-600', 'text-white', 'shadow-sm', 'font-bold');

                aplicarFiltrosMovimientos();
            });
        });
    }

    // 3. Botón de Actualizar Datos del Dashboard
    const refreshBtn = document.querySelector('[data-admin-action="refresh"]');
    if (refreshBtn) {
        refreshBtn.addEventListener('click', function () {
            const icon = this.querySelector('[data-lucide="refresh-cw"]');
            if (icon) {
                icon.classList.add('animate-spin');
            }
            this.disabled = true;
            setTimeout(() => {
                window.location.reload();
            }, 300);
        });
    }
}

document.addEventListener('DOMContentLoaded', initAdminPanel);

