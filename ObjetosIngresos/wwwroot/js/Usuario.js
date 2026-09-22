window.confirmarEliminacion = function (id, nombre) {
    Swal.fire({
        title: '<span class="text-slate-800">¿Eliminar usuario?</span>',
        html: `Vas a eliminar a <b>${nombre}</b>.<br><span class="text-sm text-slate-500">Esta acción no se puede deshacer.</span>`,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#4f46e5', 
        cancelButtonColor: '#f43f5e',  
        confirmButtonText: 'Sí, eliminar',
        cancelButtonText: 'Cancelar',
        reverseButtons: true,
        background: '#ffffff',
        borderRadius: '1.5rem',
        customClass: {
            popup: 'rounded-3xl shadow-2xl border border-slate-100',
            confirmButton: 'rounded-xl px-6 py-3 font-bold',
            cancelButton: 'rounded-xl px-6 py-3 font-bold'
        }
    }).then((result) => {
        if (result.isConfirmed) {
            Swal.fire({
                title: 'Eliminando...',
                didOpen: () => { Swal.showLoading() },
                allowOutsideClick: false,
                showConfirmButton: false
            });

            document.getElementById(`form-eliminar-${id}`).submit();
        }
    });
}


document.addEventListener('DOMContentLoaded', () => {
    const carrier = document.getElementById('validation-errors-carrier');

    if (carrier) {
        const errorsRaw = carrier.getAttribute('data-errors');
        console.log("Contenido crudo de errores:", errorsRaw);

        if (errorsRaw) {
            try {
                const errores = JSON.parse(errorsRaw);
                console.log("Errores parseados:", errores);

                if (Array.isArray(errores) && errores.length > 0) {
                    const tieneErrorCorreo = errores.some(e => e.toLowerCase().includes('correo'));
                    const tieneErrorDocumento = errores.some(e => e.toLowerCase().includes('documento'));

                    if (tieneErrorCorreo) {
                        marcarCampoConError('Correo');
                    }
                    if (tieneErrorDocumento) {
                        marcarCampoConError('Documento');
                    }

                    if (typeof Swal !== 'undefined') {
                        Swal.fire({
                            title: '<span class="text-slate-800">Atención</span>',
                            html: `<div class="text-slate-600 text-sm mt-2">${errores.join('<br>')}</div>`,
                            icon: 'warning',
                            confirmButtonColor: '#4f46e5',
                            confirmButtonText: 'Entendido',
                            background: '#ffffff',
                            borderRadius: '1.5rem',
                            customClass: {
                                popup: 'rounded-3xl shadow-2xl border border-slate-100',
                                confirmButton: 'rounded-xl px-6 py-3 font-bold'
                            }
                        });
                    } else {
                        console.error("SweetAlert2 (Swal) no está cargado.");
                        alert(errores.join('\n'));
                    }
                }
            } catch (e) {
                console.error("Error parseando el JSON de errores:", e);
            }
        }
    }
});
function marcarCampoConError(fieldId) {
    const input = document.getElementById(fieldId);
    if (input) {
        console.log(`Marcando en rojo el campo: ${fieldId}`);
        input.classList.add('border-red-500', 'ring-2', 'ring-red-200');
        input.focus();
        input.addEventListener('input', function limpiarError() {
            input.classList.remove('border-red-500', 'ring-2', 'ring-red-200');
            input.removeEventListener('input', limpiarError);
        });
    } else {
        console.warn(`No se encontró el input con id="${fieldId}" en el DOM.`);
    }
}

window.confirmarEliminacion = function (id, nombre) {
    Swal.fire({
        title: '<span class="text-slate-800">¿Eliminar usuario?</span>',
        html: `Vas a eliminar a <b>${nombre}</b>.<br><span class="text-sm text-slate-500">Esta acción no se puede deshacer.</span>`,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#4f46e5', 
        cancelButtonColor: '#f43f5e',  
        confirmButtonText: 'Sí, eliminar',
        cancelButtonText: 'Cancelar',
        reverseButtons: true,
        background: '#ffffff',
        borderRadius: '1.5rem',
        customClass: {
            popup: 'rounded-3xl shadow-2xl border border-slate-100',
            confirmButton: 'rounded-xl px-6 py-3 font-bold',
            cancelButton: 'rounded-xl px-6 py-3 font-bold'
        }
    }).then((result) => {
        if (result.isConfirmed) {
            Swal.fire({
                title: 'Eliminando...',
                didOpen: () => { Swal.showLoading() },
                allowOutsideClick: false,
                showConfirmButton: false
            });

            document.getElementById(`form-eliminar-${id}`).submit();
        }
    });
}