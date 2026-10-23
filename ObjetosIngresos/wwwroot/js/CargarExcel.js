document.addEventListener('DOMContentLoaded', function () {
    const inputBuscar = document.getElementById('buscarDocumento');
    const filas = document.querySelectorAll('.fila-usuario');
    const sinResultados = document.getElementById('sinResultados');
    const contador = document.getElementById('contadorUsuarios');

    inputBuscar.addEventListener('input', function () {
        const valorBusqueda = this.value.trim();
        let visibles = 0;

        filas.forEach(fila => {
            const documento = fila.getAttribute('data-documento') || '';

            if (documento.includes(valorBusqueda)) {
                fila.style.display = '';
                visibles++;
            } else {
                fila.style.display = 'none';
            }
        });

        if (visibles === 0 && valorBusqueda !== '') {
            sinResultados.classList.remove('hidden');
        } else {
            sinResultados.classList.add('hidden');
        }

        contador.textContent = `Mostrando ${visibles} usuarios filtrados`;
    });
});

function abrirModalImportar() {
    document.getElementById('modalImportarExcel').classList.remove('hidden');
}

function cerrarModalImportar() {
    document.getElementById('modalImportarExcel').classList.add('hidden');
    document.getElementById('formImportarExcel').reset();
    document.getElementById('nombreArchivoSeleccionado').textContent = 'XLSX o XLS hasta 10MB';
}

function actualizarNombreArchivo(input) {
    const label = document.getElementById('nombreArchivoSeleccionado');
    if (input.files && input.files[0]) {
        label.textContent = input.files[0].name;
        label.classList.add('font-semibold', 'text-indigo-600');
    } else {
        label.textContent = 'XLSX o XLS hasta 10MB';
        label.classList.remove('font-semibold', 'text-indigo-600');
    }
}

function procesarEnvioImportacion(e) {
    const btn = document.getElementById('btnSubmitImportar');
    const txt = document.getElementById('txtSubmitImportar');
    const spinner = document.getElementById('spinnerImportar');

    btn.disabled = true;
    txt.textContent = 'Procesando...';
    spinner.classList.remove('hidden');
}