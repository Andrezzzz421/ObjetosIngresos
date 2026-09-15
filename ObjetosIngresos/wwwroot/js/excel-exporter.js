/**
 * Descarga reportes Excel vía Fetch y maneja el estado visual de la UI.
 * @param {string} urlEndpoint - Ruta del Controller (e.g. '/Elementos/ExportarExcel')
 * @param {string} [btnId] - ID opcional del botón que disparó la acción
 */
async function exportarModuloAExcel(urlEndpoint, btnId = null) {
    let btn = btnId ? document.getElementById(btnId) : null;
    let overlay = document.getElementById('loadingOverlay');

    // 1. Crear el Overlay en el DOM dinámicamente si no existe en la vista
    if (!overlay) {
        overlay = document.createElement('div');
        overlay.id = 'loadingOverlay';
        overlay.className = 'fixed inset-0 bg-slate-900/50 backdrop-blur-sm hidden items-center justify-center z-50 transition-opacity';
        overlay.innerHTML = `
            <div class="bg-white p-6 rounded-2xl shadow-xl flex flex-col items-center gap-4 max-w-sm mx-auto text-center border border-slate-100">
                <svg class="animate-spin h-10 w-10 text-emerald-600" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                    <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                    <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                </svg>
                <div>
                    <h3 class="text-base font-bold text-slate-800">Generando reporte Excel</h3>
                    <p class="text-xs text-slate-500 mt-1">Por favor espera un momento mientras procesamos los datos...</p>
                </div>
            </div>
        `;
        document.body.appendChild(overlay);
    }

    // 2. Mostrar Spinner y Deshabilitar Botón
    overlay.classList.remove('hidden');
    overlay.classList.add('flex');

    if (btn) {
        btn.disabled = true;
        btn.classList.add('opacity-50', 'cursor-not-allowed');
    }

    try {
        const response = await fetch(urlEndpoint, { method: 'GET' });

        if (!response.ok) {
            throw new Error('Error al generar el reporte en el servidor.');
        }

        const blob = await response.blob();
        const contentDisposition = response.headers.get('Content-Disposition');
        let filename = 'Reporte_Exportacion.xlsx';

        if (contentDisposition && contentDisposition.includes('filename=')) {
            filename = contentDisposition.split('filename=')[1].replace(/"/g, '');
        }

        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = filename;
        document.body.appendChild(a);
        a.click();

        a.remove();
        window.URL.revokeObjectURL(url);
    } catch (error) {
        console.error(error);
        alert('Ocurrió un problema al intentar descargar el archivo Excel.');
    } finally {
        // 3. Ocultar Spinner y Rehabilitar Botón
        overlay.classList.add('hidden');
        overlay.classList.remove('flex');

        if (btn) {
            btn.disabled = false;
            btn.classList.remove('opacity-50', 'cursor-not-allowed');
        }
    }
}

async function descargarExcelOptimizado() {
    const overlay = document.getElementById('loadingOverlay');
    const btn = document.getElementById('btnExportarExcel');

    overlay.classList.remove('hidden');
    overlay.classList.add('flex');
    btn.disabled = true;
    btn.classList.add('opacity-50', 'cursor-not-allowed');

    try {
        const response = await fetch('@Url.Action("ExportarExcel", "Elementos")', {
            method: 'GET'
        });

        if (!response.ok) {
            throw new Error('Error al generar el reporte en el servidor.');
        }

        const blob = await response.blob();

        const contentDisposition = response.headers.get('Content-Disposition');
        let filename = 'Inventario_Elementos.xlsx';
        if (contentDisposition && contentDisposition.includes('filename=')) {
            filename = contentDisposition.split('filename=')[1].replace(/"/g, '');
        }

        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = filename;
        document.body.appendChild(a);
        a.click();

        a.remove();
        window.URL.revokeObjectURL(url);
    } catch (error) {
        console.error(error);
        alert('Ocurrió un problema al intentar descargar el archivo Excel.');
    } finally {
        overlay.classList.add('hidden');
        overlay.classList.remove('flex');
        btn.disabled = false;
        btn.classList.remove('opacity-50', 'cursor-not-allowed');
    }
}
