document.addEventListener('DOMContentLoaded', () => {
    // --- ESTADO GLOBAL ---
    const estado = {
        equipos: [],
        detallesTemp: [],
        imagenTempFile: null,
        fotoAccesorioTempFile: null
    };

    // --- ELEMENTOS DEL DOM ---
    const dom = {
        // Formulario y Contenedores
        formFinal: document.getElementById('form-registro-final'),
        contenedorAgregados: document.getElementById('contenedor-equipos-agregados'),
        contenedorOcultos: document.getElementById('inputs-ocultos-container'),
        mensajeVacio: document.getElementById('mensaje-vacio'),
        badgeContador: document.getElementById('badge-contador'),
        btnGuardarTodo: document.getElementById('btn-guardar-todo'),

        // Campos Borrador: Equipo
        fotoInput: document.getElementById('foto'),
        previewImg: document.getElementById('preview'),
        uploadPlaceholder: document.getElementById('upload-placeholder'),
        tipoSelect: document.getElementById('temp-tipo'),
        otroTipoContainer: document.getElementById('contenedor-otro-temp'),
        otroTipoInput: document.getElementById('temp-otro-tipo'),
        marcaSelect: document.getElementById('temp-marca'),
        serialInput: document.getElementById('temp-serial'),
        btnAddEquipo: document.getElementById('btn-add-equipo'),

        // Campos Borrador: Accesorios
        accesorioSelect: document.getElementById('temp-tipo-accesorio'),
        otroAccesorioContainer: document.getElementById('contenedor-otro-accesorio'),
        nuevoAccesorioInput: document.getElementById('temp-nuevo-accesorio'),
        detalleInput: document.getElementById('temp-detalle-input'),
        listaDetallesTemp: document.getElementById('lista-detalles-temp'),
        emptyDetallesMsg: document.getElementById('empty-detalles-msg'),
        btnAddDetalle: document.getElementById('btn-add-detalle'),

        // Foto Accesorio
        fotoAccesorioInput: document.getElementById('temp-foto-accesorio'),
        lblFotoAccesorio: document.getElementById('lbl-foto-accesorio'),
        txtFotoAccesorio: document.getElementById('txt-foto-accesorio'),
        previewBoxAccesorio: document.getElementById('preview-box-accesorio'),
        previewImgAccesorio: document.getElementById('preview-img-accesorio'),
        btnQuitarFotoAccesorio: document.getElementById('btn-quitar-foto-accesorio')
    };

    // --- EVENT LISTENERS ---
    const inicializarEventos = () => {
        // Manejo de Vista Previa de Imagen del Equipo
        dom.fotoInput?.addEventListener('change', manejarCambioImagen);

        // Manejo de Foto del Accesorio
        dom.fotoAccesorioInput?.addEventListener('change', manejarCambioFotoAccesorio);
        dom.btnQuitarFotoAccesorio?.addEventListener('click', resetearFotoAccesorio);

        // Conmutadores para opciones "Otro"
        dom.tipoSelect?.addEventListener('change', () => {
            dom.otroTipoContainer?.classList.toggle('hidden', dom.tipoSelect.value !== 'Otro');
        });

        dom.accesorioSelect?.addEventListener('change', () => {
            const esOtro = dom.accesorioSelect.value === 'Otro';
            if (dom.otroAccesorioContainer) {
                dom.otroAccesorioContainer.style.display = esOtro ? 'block' : 'none';
            }
        });

        // Acciones de Adición
        dom.btnAddDetalle?.addEventListener('click', agregarAccesorioTemporal);
        dom.btnAddEquipo?.addEventListener('click', agregarEquipoAEstado);

        // Delegación de eventos para eliminar items dinámicos
        dom.listaDetallesTemp?.addEventListener('click', e => {
            const btn = e.target.closest('.btn-del-detalle');
            if (btn) eliminarAccesorioTemporal(parseInt(btn.dataset.index, 10));
        });

        dom.contenedorAgregados?.addEventListener('click', e => {
            const btn = e.target.closest('.btn-del-equipo');
            if (btn) eliminarEquipoDelEstado(parseInt(btn.dataset.index, 10));
        });
    };

    // --- MANEJO DE IMÁGENES EQUIPO ---
    const manejarCambioImagen = (e) => {
        const file = e.target.files[0];
        if (!file) return;

        estado.imagenTempFile = file;
        const reader = new FileReader();

        reader.onload = (evt) => {
            dom.previewImg.src = evt.target.result;
            dom.previewImg.classList.remove('hidden');
            dom.uploadPlaceholder?.classList.add('hidden');
        };

        reader.readAsDataURL(file);
    };

    const resetearVistaPreviaImagen = () => {
        dom.fotoInput.value = '';
        dom.previewImg.src = '';
        dom.previewImg.classList.add('hidden');
        dom.uploadPlaceholder?.classList.remove('hidden');
        estado.imagenTempFile = null;
    };

    // --- MANEJO DE IMAGEN ACCESORIO ---
    const manejarCambioFotoAccesorio = (e) => {
        const file = e.target.files[0];
        if (!file) return;

        estado.fotoAccesorioTempFile = file;
        const objectUrl = URL.createObjectURL(file);
        estado.fotoAccesorioTempUrl = objectUrl;

        if (dom.previewImgAccesorio) dom.previewImgAccesorio.src = objectUrl;
        dom.previewBoxAccesorio?.classList.remove('hidden');
        if (dom.txtFotoAccesorio) dom.txtFotoAccesorio.textContent = 'Cambiar';
        dom.lblFotoAccesorio?.classList.add('border-indigo-400', 'bg-indigo-50/50');
    };

    const resetearFotoAccesorio = () => {
        if (dom.fotoAccesorioInput) dom.fotoAccesorioInput.value = '';
        if (dom.previewImgAccesorio) dom.previewImgAccesorio.src = '';
        dom.previewBoxAccesorio?.classList.add('hidden');
        if (dom.txtFotoAccesorio) dom.txtFotoAccesorio.textContent = 'Foto';
        dom.lblFotoAccesorio?.classList.remove('border-indigo-400', 'bg-indigo-50/50');
        estado.fotoAccesorioTempFile = null;
        estado.fotoAccesorioTempUrl = null;
    };

    // --- LÓGICA DE ACCESORIOS TEMPORALES ---
    const agregarAccesorioTemporal = () => {
        const selectVal = dom.accesorioSelect.value;
        if (!selectVal) return;

        let nombre = '';
        let idTipo = null;

        if (selectVal === 'Otro') {
            nombre = dom.nuevoAccesorioInput.value.trim();
            if (!nombre) {
                alert('Por favor escribe el nombre del accesorio.');
                return;
            }
        } else {
            nombre = dom.accesorioSelect.options[dom.accesorioSelect.selectedIndex].text;
            idTipo = parseInt(selectVal, 10);
        }

        const fotoFile = estado.fotoAccesorioTempFile;
        const fotoUrl = estado.fotoAccesorioTempUrl || (dom.previewImgAccesorio && !dom.previewImgAccesorio.classList.contains('hidden') ? dom.previewImgAccesorio.src : null);

        estado.detallesTemp.push({
            idTipoDetalle: idTipo,
            nombre: nombre,
            detalle: dom.detalleInput.value.trim(),
            fotoFile: fotoFile,
            fotoUrl: fotoUrl
        });

        // Limpiar controles
        dom.accesorioSelect.value = '';
        dom.nuevoAccesorioInput.value = '';
        dom.detalleInput.value = '';
        if (dom.otroAccesorioContainer) dom.otroAccesorioContainer.style.display = 'none';
        resetearFotoAccesorio();

        renderizarAccesoriosTemporales();
    };

    const eliminarAccesorioTemporal = (index) => {
        estado.detallesTemp.splice(index, 1);
        renderizarAccesoriosTemporales();
    };

    const renderizarAccesoriosTemporales = () => {
        dom.listaDetallesTemp.innerHTML = '';

        if (estado.detallesTemp.length === 0) {
            dom.listaDetallesTemp.appendChild(dom.emptyDetallesMsg);
            return;
        }

        estado.detallesTemp.forEach((acc, i) => {
            const tag = document.createElement('span');
            tag.className = 'inline-flex items-center gap-2 px-3 py-1.5 bg-white border border-slate-200 text-slate-700 text-xs rounded-xl shadow-sm';

            const miniFoto = acc.fotoUrl
                ? `<img src="${acc.fotoUrl}" class="w-6 h-6 rounded-md object-cover border border-indigo-200 shrink-0" title="Foto del accesorio" />`
                : `<span class="w-6 h-6 rounded-md bg-slate-100 flex items-center justify-center text-slate-400 text-[10px]" title="Sin foto">📷</span>`;

            tag.innerHTML = `
                ${miniFoto}
                <span class="font-semibold text-slate-800">${escaparHtml(acc.nombre)}</span>
                ${acc.detalle ? `<span class="text-slate-400">(${escaparHtml(acc.detalle)})</span>` : ''}
                <button type="button" data-index="${i}" class="btn-del-detalle text-slate-400 hover:text-red-500 font-bold ml-1" title="Eliminar">
                    &times;
                </button>
            `;
            dom.listaDetallesTemp.appendChild(tag);
        });
    };

    // --- LÓGICA DE EQUIPOS ---
    const agregarEquipoAEstado = () => {
        const tipoVal = dom.tipoSelect.value;
        if (!tipoVal) {
            alert('Por favor selecciona un tipo de elemento.');
            return;
        }

        const tipoFinal = tipoVal === 'Otro' ? dom.otroTipoInput.value.trim() : tipoVal;
        if (!tipoFinal) return;

        // Si el usuario seleccionó un accesorio pero no presionó "+ Añadir", añadirlo automáticamente
        if (dom.accesorioSelect && dom.accesorioSelect.value) {
            agregarAccesorioTemporal();
        } else if (estado.fotoAccesorioTempFile || (dom.detalleInput && dom.detalleInput.value.trim())) {
            alert('Has seleccionado una foto o nota de accesorio pero no elegiste el accesorio. Por favor selecciona el tipo de accesorio (ej: Cargador) para incluirlo.');
            return;
        }

        const marcaText = dom.marcaSelect.options[dom.marcaSelect.selectedIndex]?.text || 'Sin marca';
        const marcaVal = dom.marcaSelect.value;

        const nuevoEquipo = {
            tipo: tipoFinal,
            idMarca: marcaVal ? parseInt(marcaVal, 10) : null,
            marcaTexto: marcaVal ? marcaText : 'Sin marca',
            serial: dom.serialInput.value.trim(),
            fotoFile: estado.imagenTempFile,
            fotoPreviewUrl: dom.previewImg.src || null,
            detalles: [...estado.detallesTemp]
        };

        estado.equipos.push(nuevoEquipo);

        // Resetear Formulario Borrador
        dom.tipoSelect.value = '';
        dom.otroTipoInput.value = '';
        dom.otroTipoContainer?.classList.add('hidden');
        dom.marcaSelect.value = '';
        dom.serialInput.value = '';
        estado.detallesTemp = [];
        resetearVistaPreviaImagen();
        resetearFotoAccesorio();
        renderizarAccesoriosTemporales();

        // Actualizar UI General
        sincronizarEstadoConUI();
    };

    const eliminarEquipoDelEstado = (index) => {
        estado.equipos.splice(index, 1);
        sincronizarEstadoConUI();
    };

    // --- SINCRONIZACIÓN Y RENDERIZADO GLOBAL ---
    const sincronizarEstadoConUI = () => {
        renderizarListaEquipos();
        generarInputsOcultos();

        const total = estado.equipos.length;
        dom.badgeContador.textContent = `${total} ${total === 1 ? 'equipo' : 'equipos'}`;
        dom.btnGuardarTodo.disabled = total === 0;
        dom.mensajeVacio?.classList.toggle('hidden', total > 0);
    };

    const renderizarListaEquipos = () => {
        dom.contenedorAgregados.innerHTML = '';

        if (estado.equipos.length === 0) {
            dom.contenedorAgregados.appendChild(dom.mensajeVacio);
            return;
        }

        estado.equipos.forEach((eq, i) => {
            const card = document.createElement('div');
            card.className = 'flex items-start justify-between p-4 bg-slate-50 rounded-2xl border border-slate-200 gap-4';

            const tieneFoto = Boolean(eq.fotoPreviewUrl);
            const fotoHTML = tieneFoto
                ? `<img src="${eq.fotoPreviewUrl}" class="w-16 h-16 object-cover rounded-xl border border-slate-200 shrink-0" />`
                : `<div class="w-16 h-16 bg-slate-200 text-slate-400 rounded-xl flex items-center justify-center shrink-0 font-bold text-xs">Sin Foto</div>`;

            const detallesHTML = eq.detalles.length > 0
                ? eq.detalles.map(d => {
                    const thumb = d.fotoUrl
                        ? `<img src="${d.fotoUrl}" class="w-6 h-6 rounded-md object-cover inline-block border border-indigo-200 mr-1.5 align-middle shadow-sm" />`
                        : `<span class="inline-block mr-1">📦</span>`;
                    const nota = d.detalle ? ` <span class="text-slate-400 font-normal">(${escaparHtml(d.detalle)})</span>` : '';
                    return `<span class="inline-flex items-center bg-white px-2.5 py-1 rounded-xl border border-slate-200 text-xs font-semibold text-slate-700 mr-1.5 mt-1 shadow-xs">${thumb}${escaparHtml(d.nombre)}${nota}</span>`;
                }).join('')
                : '<span class="text-xs text-slate-400 italic">Sin accesorios</span>';

            card.innerHTML = `
                <div class="flex items-start gap-4">
                    ${fotoHTML}
                    <div>
                        <h4 class="font-bold text-slate-800 text-base">${escaparHtml(eq.tipo)}</h4>
                        <p class="text-xs text-slate-500">
                            Marca: <span class="font-medium text-slate-700">${escaparHtml(eq.marcaTexto)}</span> | 
                            Serie: <span class="font-medium text-slate-700">${escaparHtml(eq.serial || 'N/A')}</span>
                        </p>
                        <div class="mt-2 flex flex-wrap gap-1">${detallesHTML}</div>
                    </div>
                </div>
                <button type="button" data-index="${i}" class="btn-del-equipo text-slate-400 hover:text-red-600 p-2 transition-colors" title="Eliminar de la lista">
                    <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"/>
                    </svg>
                </button>
            `;
            dom.contenedorAgregados.appendChild(card);
        });
    };

    // --- CONSTRUCCIÓN DEL FORMULARIO HTTP POST (Model Binding para ASP.NET Core) ---
    const generarInputsOcultos = () => {
        dom.contenedorOcultos.innerHTML = '';
        const dataTransfer = new DataTransfer();

        estado.equipos.forEach((eq, i) => {
            // 1. Inputs del Equipo
            crearHiddenInput(`Equipos[${i}].TipoElemento`, eq.tipo);
            if (eq.idMarca) crearHiddenInput(`Equipos[${i}].IdMarca`, eq.idMarca);
            if (eq.serial) crearHiddenInput(`Equipos[${i}].NumeroSerie`, eq.serial);

            // 2. Mantenimiento del índice exacto para Fotos del Equipo
            if (eq.fotoFile) {
                const fileRenombrado = new File([eq.fotoFile], `equipo_${i}_${eq.fotoFile.name}`, { type: eq.fotoFile.type });
                dataTransfer.items.add(fileRenombrado);
            } else {
                const dummyFile = new File([""], `empty_${i}.txt`, { type: "text/plain" });
                dataTransfer.items.add(dummyFile);
            }

            // 3. Accesorios
            eq.detalles.forEach((det, j) => {
                const idTipoFinal = det.idTipoDetalle ? det.idTipoDetalle : 0;

                crearHiddenInput(`Equipos[${i}].Detalles[${j}].IdTipoDetalle`, idTipoFinal);
                crearHiddenInput(`Equipos[${i}].Detalles[${j}].Nombre`, det.nombre);
                if (det.detalle) {
                    crearHiddenInput(`Equipos[${i}].Detalles[${j}].Especificacion`, det.detalle);
                }

                // Si el accesorio tiene foto tomada por el usuario
                if (det.fotoFile) {
                    const inputAccFoto = document.createElement('input');
                    inputAccFoto.type = 'file';
                    inputAccFoto.name = `foto_acc_${i}_${j}`;
                    inputAccFoto.className = 'hidden';
                    const dtAcc = new DataTransfer();
                    dtAcc.items.add(det.fotoFile);
                    inputAccFoto.files = dtAcc.files;
                    dom.contenedorOcultos.appendChild(inputAccFoto);
                }
            });
        });

        // Adjuntar las fotos al input de archivos de equipos
        if (dataTransfer.files.length > 0) {
            const inputFotosOculto = document.createElement('input');
            inputFotosOculto.type = 'file';
            inputFotosOculto.name = 'FotosEquipos';
            inputFotosOculto.multiple = true;
            inputFotosOculto.className = 'hidden';

            dom.contenedorOcultos.appendChild(inputFotosOculto);
            inputFotosOculto.files = dataTransfer.files;
        }
    };

    const crearHiddenInput = (name, value) => {
        const input = document.createElement('input');
        input.type = 'hidden';
        input.name = name;
        input.value = value;
        dom.contenedorOcultos.appendChild(input);
    };

    const escaparHtml = (str) => {
        if (!str) return '';
        return str.replace(/[&<>"']/g, (m) => ({
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            '"': '&quot;',
            "'": '&#039;'
        })[m]);
    };

    // Inicializar la aplicación
    inicializarEventos();
});