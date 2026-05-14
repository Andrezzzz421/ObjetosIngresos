// Función global para confirmar la eliminación
window.confirmarEliminacion = function (id, nombre) {
    Swal.fire({
        title: '<span class="text-slate-800">¿Eliminar usuario?</span>',
        html: `Vas a eliminar a <b>${nombre}</b>.<br><span class="text-sm text-slate-500">Esta acción no se puede deshacer.</span>`,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#4f46e5', // Indigo 600
        cancelButtonColor: '#f43f5e',  // Rose 500
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
            // Animación de carga opcional
            Swal.fire({
                title: 'Eliminando...',
                didOpen: () => { Swal.showLoading() },
                allowOutsideClick: false,
                showConfirmButton: false
            });

            // Enviamos el formulario oculto que creamos en la vista
            document.getElementById(`form-eliminar-${id}`).submit();
        }
    });
}