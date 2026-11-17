using Core.Algoritmos;
using Core.Estructuras;
using Core.Grafos;
using Core.Modelos;
using ChefRoute.Maui.Graphics;
using ChefRoute.Maui.Models;
using ChefRoute.Maui.Services;

namespace ChefRoute.Maui
{
    public partial class MainPage : ContentPage
    {
        private readonly ServicioCocinaMaui _servicio;
        private readonly KitchenSceneDrawable _escenarioDrawable;
        private readonly GraphCanvasDrawable _grafoDrawable;
        private readonly IDispatcherTimer _timerAnimacion;

        // Chef movement interpolation state
        private float _chefOrigenX = 0.50f;
        private float _chefOrigenY = 0.62f;
        private float _chefDestinoX = 0.50f;
        private float _chefDestinoY = 0.62f;
        private float _progresoMovimiento = 1.0f;
        private bool _chefEnMovimiento = false;

        // Recipe draft state
        private GrafoReceta _grafoBorrador;
        private int _contadorTareasBorrador = 1;
        private int _totalTareasReceta = 0;

        public MainPage()
        {
            InitializeComponent();
            _servicio = new ServicioCocinaMaui();
            _escenarioDrawable = new KitchenSceneDrawable();
            _grafoDrawable = new GraphCanvasDrawable();
            _grafoBorrador = new GrafoReceta();

            VistaEscenarioCocina.Drawable = _escenarioDrawable;
            CanvasGrafo.Drawable = _grafoDrawable;

            // 60 FPS Animation Loop for Chef walking, steam, and station transitions
            _timerAnimacion = Dispatcher.CreateTimer();
            _timerAnimacion.Interval = TimeSpan.FromMilliseconds(16);
            _timerAnimacion.Tick += OnAnimationTick;
            _timerAnimacion.Start();

            LblCarpetaJson.Text = $"Carpeta compartida: {_servicio.ObtenerCarpetaRecetas()}";
            CargarCatalogoRecetas();
            AutoCargarPrimeraRecetaSiExiste();
            CambiarVista(1);
        }

        private void AutoCargarPrimeraRecetaSiExiste()
        {
            RecetaArchivoItem[] recetas = _servicio.ObtenerCatalogoRecetas();
            for (int i = 0; i < recetas.Length; i++)
            {
                if (!recetas[i].TieneCiclo && recetas[i].CantidadTareas > 0)
                {
                    _servicio.CargarRecetaDesdeArchivo(recetas[i].RutaCompleta);
                    ActualizarEncabezadoYTicket();
                    break;
                }
            }
        }

        // =====================================================================
        // 60 FPS ANIMATION LOOP (CHEF WALKING & KITCHEN AMBIENCE)
        // =====================================================================
        private async void OnAnimationTick(object? sender, EventArgs e)
        {
            _escenarioDrawable.FaseVapor += 0.06f;

            if (_chefEnMovimiento)
            {
                _escenarioDrawable.FaseCaminata += 0.28f;
                _progresoMovimiento += 0.025f; // ~0.65 seconds smooth walk between stations

                if (_progresoMovimiento >= 1.0f)
                {
                    _progresoMovimiento = 1.0f;
                    _chefEnMovimiento = false;
                    _escenarioDrawable.ChefNormX = _chefDestinoX;
                    _escenarioDrawable.ChefNormY = _chefDestinoY;
                    _escenarioDrawable.EstadoChef = EstadoAnimacionChef.Supervisando;
                    _escenarioDrawable.EtiquetaChef = "Supervisando";

                    VistaEscenarioCocina.Invalidate();
                    await MostrarNotaSupervisionAnimada();
                }
                else
                {
                    // Smooth ease-in-out interpolation
                    float t = _progresoMovimiento;
                    float ease = t * t * (3f - 2f * t);
                    _escenarioDrawable.ChefNormX = _chefOrigenX + (_chefDestinoX - _chefOrigenX) * ease;
                    _escenarioDrawable.ChefNormY = _chefOrigenY + (_chefDestinoY - _chefOrigenY) * ease;
                }
            }

            if (VistaCocina.IsVisible)
            {
                VistaEscenarioCocina.Invalidate();
            }
        }

        private void MoverChefHaciaEstacion(TipoEstacionCocina estacion)
        {
            var (tx, ty) = KitchenSceneDrawable.ObtenerPosicionFrenteEstacion(estacion);

            _chefOrigenX = _escenarioDrawable.ChefNormX;
            _chefOrigenY = _escenarioDrawable.ChefNormY;
            _chefDestinoX = tx;
            _chefDestinoY = ty;

            if (Math.Abs(tx - _chefOrigenX) > 0.01f)
            {
                _escenarioDrawable.ChefMirandoDerecha = tx >= _chefOrigenX;
            }

            _escenarioDrawable.EstacionActiva = estacion;
            _progresoMovimiento = 0f;
            _chefEnMovimiento = true;
            _escenarioDrawable.EstadoChef = EstadoAnimacionChef.Caminando;
            _escenarioDrawable.EtiquetaChef = "Caminando...";
            NotaSupervision.IsVisible = false;
        }

        private async Task MostrarNotaSupervisionAnimada()
        {
            if (_servicio.TareaEnFocoActual == null) return;

            Tarea t = _servicio.TareaEnFocoActual;
            TipoEstacionCocina est = ServicioCocinaMaui.ClasificarEstacionDeTarea(t);

            LblNotaEstacion.Text = KitchenSceneDrawable.ObtenerNombreEstacion(est).ToUpperInvariant();
            LblNotaTiempo.Text = $"{t.TiempoEstimado} min";
            LblNotaTarea.Text = $"[{t.Id}] {t.Nombre}";
            LblNotaDescripcion.Text = t.Descripcion;
            LblNotaEstado.Text = "Estado: Supervisando en estacion";
            LblNotaDependencias.Text = $"Dependencias: {_servicio.ObtenerDependenciasDeTarea(t)}";

            NotaSupervision.IsVisible = true;
            NotaSupervision.Opacity = 0;
            NotaSupervision.Scale = 0.92;
            await Task.WhenAll(
                NotaSupervision.FadeToAsync(1.0, 180),
                NotaSupervision.ScaleToAsync(1.0, 180)
            );
        }

        // =====================================================================
        // TOP NAVIGATION
        // =====================================================================
        private void CambiarVista(int vista)
        {
            VistaCocina.IsVisible = vista == 1;
            VistaRecetas.IsVisible = vista == 2;
            VistaGrafo.IsVisible = vista == 3;

            BtnNavCocina.BackgroundColor = vista == 1 ? Color.FromArgb("#EDE5D5") : Colors.Transparent;
            BtnNavCocina.TextColor = vista == 1 ? Color.FromArgb("#E07A5F") : Color.FromArgb("#64748B");

            BtnNavRecetas.BackgroundColor = vista == 2 ? Color.FromArgb("#EDE5D5") : Colors.Transparent;
            BtnNavRecetas.TextColor = vista == 2 ? Color.FromArgb("#E07A5F") : Color.FromArgb("#64748B");

            BtnNavGrafo.BackgroundColor = vista == 3 ? Color.FromArgb("#EDE5D5") : Colors.Transparent;
            BtnNavGrafo.TextColor = vista == 3 ? Color.FromArgb("#E07A5F") : Color.FromArgb("#64748B");

            if (vista == 1)
            {
                ActualizarEncabezadoYTicket();
                VistaEscenarioCocina.Invalidate();
            }
            else if (vista == 3)
            {
                CargarDatosEnCanvasGrafo();
            }
        }

        private void OnNavCocinaClicked(object sender, EventArgs e) => CambiarVista(1);
        private void OnNavRecetasClicked(object sender, EventArgs e) => CambiarVista(2);
        private void OnNavGrafoClicked(object sender, EventArgs e) => CambiarVista(3);

        // =====================================================================
        // KITCHEN SUPERVISION & CIRCULAR LINKED LIST SYNCHRONIZATION
        // =====================================================================
        private void OnIniciarRondaClicked(object sender, EventArgs e)
        {
            try
            {
                BannerCocina.IsVisible = false;
                ListaSimple<Tarea> plan = _servicio.IniciarSupervisionEnCocina();
                _totalTareasReceta = plan.cantidad;

                BtnAvanzarChef.IsEnabled = true;
                BtnCompletarEstacion.IsEnabled = true;

                ActualizarEncabezadoYTicket();

                if (_servicio.TareaEnFocoActual != null)
                {
                    TipoEstacionCocina est = ServicioCocinaMaui.ClasificarEstacionDeTarea(_servicio.TareaEnFocoActual);
                    MoverChefHaciaEstacion(est);
                }
            }
            catch (Exception ex)
            {
                MostrarBannerCocina(ex.Message, true);
            }
        }

        private void OnAvanzarChefClicked(object sender, EventArgs e)
        {
            try
            {
                BannerCocina.IsVisible = false;
                // Advances the real ListaCircular<Tarea> in Core!
                Tarea siguiente = _servicio.AvanzarNodoCircular();
                ActualizarEncabezadoYTicket();

                TipoEstacionCocina est = ServicioCocinaMaui.ClasificarEstacionDeTarea(siguiente);
                MoverChefHaciaEstacion(est);
            }
            catch (Exception ex)
            {
                MostrarBannerCocina(ex.Message, true);
            }
        }

        private async void OnCompletarEstacionClicked(object sender, EventArgs e)
        {
            try
            {
                BannerCocina.IsVisible = false;
                string nombreTarea = _servicio.TareaEnFocoActual?.Nombre ?? "Tarea";

                // Validates prerequisites in Core.SupervisorReceta and removes node from ListaCircular
                _servicio.CompletarTareaEnFoco();
                ActualizarEncabezadoYTicket();

                if (_servicio.RecetaCompletada)
                {
                    NotaSupervision.IsVisible = false;
                    BtnAvanzarChef.IsEnabled = false;
                    BtnCompletarEstacion.IsEnabled = false;
                    _escenarioDrawable.EstacionActiva = null;
                    _escenarioDrawable.EstadoChef = EstadoAnimacionChef.RecetaTerminada;
                    _escenarioDrawable.EtiquetaChef = "Plato Terminado!";
                    VistaEscenarioCocina.Invalidate();

                    MostrarBannerCocina($"Plato '{_servicio.NombreRecetaActual}' completado y listo para servir!", false);
                }
                else if (_servicio.TareaEnFocoActual != null)
                {
                    MostrarBannerCocina($"Tarea '{nombreTarea}' completada. Avanzando en la ronda circular...", false);
                    TipoEstacionCocina est = ServicioCocinaMaui.ClasificarEstacionDeTarea(_servicio.TareaEnFocoActual);
                    MoverChefHaciaEstacion(est);
                }
            }
            catch (Exception ex)
            {
                // Prerequisite block alert on Chef + visual shake
                _escenarioDrawable.EstadoChef = EstadoAnimacionChef.AlertaBloqueo;
                _escenarioDrawable.EtiquetaChef = "Falta Prerrequisito!";
                VistaEscenarioCocina.Invalidate();

                await VistaEscenarioCocina.TranslateToAsync(-8, 0, 50);
                await VistaEscenarioCocina.TranslateToAsync(8, 0, 50);
                await VistaEscenarioCocina.TranslateToAsync(0, 0, 50);

                MostrarBannerCocina(ex.Message, true);
            }
        }

        private void MostrarBannerCocina(string mensaje, bool esError)
        {
            LblBannerCocina.Text = mensaje;
            BannerCocina.Stroke = esError ? Color.FromArgb("#DC2626") : Color.FromArgb("#16A34A");
            LblBannerCocina.TextColor = esError ? Color.FromArgb("#DC2626") : Color.FromArgb("#16A34A");
            BannerCocina.IsVisible = true;
        }

        // =====================================================================
        // RESTAURANT TICKET (COMANDA INFERIOR 30%) RENDERING
        // =====================================================================
        private void ActualizarEncabezadoYTicket()
        {
            LblTopRecetaActiva.Text = _servicio.HayRecetaCargada
                ? $"Plato: {_servicio.NombreRecetaActual}"
                : "Plato: Ninguno";

            ContenedorTareasTicket.Children.Clear();
            _escenarioDrawable.TotalPorEstacion.Clear();
            _escenarioDrawable.CompletadasPorEstacion.Clear();

            if (!_servicio.HayRecetaCargada)
            {
                LblTicketPlato.Text = "SIN PLATO SELECCIONADO";
                LblTicketDescripcion.Text = "Abre una receta desde 'Recetas y Estudio'.";
                LblTicketMeta.Text = "TIEMPO EST: 0 MIN  |  TAREAS: 0";
                LblTicketProgreso.Text = "0/0 LISTO";
                return;
            }

            TareaTicketItem[] items = _servicio.ObtenerItemsTicket();
            int sumaMinutos = 0;
            int completadas = 0;

            for (int i = 0; i < items.Length; i++)
            {
                TareaTicketItem it = items[i];
                sumaMinutos += it.TiempoEstimado;

                if (!_escenarioDrawable.TotalPorEstacion.ContainsKey(it.Estacion))
                {
                    _escenarioDrawable.TotalPorEstacion[it.Estacion] = 0;
                    _escenarioDrawable.CompletadasPorEstacion[it.Estacion] = 0;
                }
                _escenarioDrawable.TotalPorEstacion[it.Estacion]++;

                if (it.Estado == EstadoTareaVisual.Completada)
                {
                    completadas++;
                    _escenarioDrawable.CompletadasPorEstacion[it.Estacion]++;
                }

                // Build compact restaurant order ticket line item
                Color colorEstado = it.Estado switch
                {
                    EstadoTareaVisual.Completada  => Color.FromArgb("#16A34A"),
                    EstadoTareaVisual.EnEjecucion => Color.FromArgb("#2563EB"),
                    EstadoTareaVisual.Bloqueada   => Color.FromArgb("#DC2626"),
                    _ => Color.FromArgb("#D97706")
                };

                string simbolo = it.Estado switch
                {
                    EstadoTareaVisual.Completada  => "[✓]",
                    EstadoTareaVisual.EnEjecucion => "[►]",
                    EstadoTareaVisual.Bloqueada   => "[!]",
                    _ => "[ ]"
                };

                var rowBorder = new Border
                {
                    BackgroundColor = it.EsTareaActual ? Color.FromArgb("#DBEAFE") : Color.FromArgb("#FAF7F0"),
                    Stroke = it.EsTareaActual ? Color.FromArgb("#2563EB") : Color.FromArgb("#E7E0D2"),
                    StrokeThickness = it.EsTareaActual ? 1.5 : 1,
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 6 },
                    Padding = new Thickness(10, 5)
                };

                var rowGrid = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition(new GridLength(36)),
                        new ColumnDefinition(GridLength.Star),
                        new ColumnDefinition(GridLength.Auto),
                        new ColumnDefinition(new GridLength(64))
                    },
                    ColumnSpacing = 8
                };

                rowGrid.Add(new Label
                {
                    Text = simbolo,
                    FontFamily = "Consolas",
                    FontSize = 12,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = colorEstado,
                    VerticalOptions = LayoutOptions.Center
                }, 0, 0);

                rowGrid.Add(new Label
                {
                    Text = $"[{it.Id}] {it.Nombre}  •  ({it.NombreEstacion})",
                    FontFamily = "Consolas",
                    FontSize = 12,
                    FontAttributes = it.EsTareaActual ? FontAttributes.Bold : FontAttributes.None,
                    TextColor = Color.FromArgb("#1E293B"),
                    LineBreakMode = LineBreakMode.TailTruncation,
                    VerticalOptions = LayoutOptions.Center
                }, 1, 0);

                rowGrid.Add(new Label
                {
                    Text = it.DependenciasTexto,
                    FontFamily = "Consolas",
                    FontSize = 11,
                    TextColor = colorEstado,
                    VerticalOptions = LayoutOptions.Center
                }, 2, 0);

                rowGrid.Add(new Label
                {
                    Text = $"{it.TiempoEstimado} min",
                    FontFamily = "Consolas",
                    FontSize = 12,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#1E293B"),
                    HorizontalTextAlignment = TextAlignment.End,
                    VerticalOptions = LayoutOptions.Center
                }, 3, 0);

                rowBorder.Content = rowGrid;
                ContenedorTareasTicket.Children.Add(rowBorder);
            }

            LblTicketPlato.Text = _servicio.NombreRecetaActual.ToUpperInvariant();
            LblTicketDescripcion.Text = _servicio.DescripcionRecetaActual;
            LblTicketMeta.Text = $"TIEMPO EST: {sumaMinutos} MIN  |  TAREAS: {items.Length}";
            LblTicketProgreso.Text = $"{completadas}/{items.Length} LISTO";
        }

        // =====================================================================
        // VIEW 2: RECETAS & ESTUDIO DE CREACION
        // =====================================================================
        private void OnRefrescarRecetasClicked(object sender, EventArgs e)
        {
            CargarCatalogoRecetas();
        }

        private void CargarCatalogoRecetas()
        {
            ContenedorCatalogoRecetas.Children.Clear();
            RecetaArchivoItem[] recetas = _servicio.ObtenerCatalogoRecetas();

            if (recetas.Length == 0)
            {
                ContenedorCatalogoRecetas.Children.Add(new Label
                {
                    Text = "No hay recetas JSON en la carpeta. Crea una receta nueva a la derecha.",
                    TextColor = Color.FromArgb("#64748B")
                });
                return;
            }

            for (int i = 0; i < recetas.Length; i++)
            {
                RecetaArchivoItem item = recetas[i];

                var card = new Border
                {
                    BackgroundColor = Color.FromArgb("#FAF7F0"),
                    Stroke = item.TieneCiclo ? Color.FromArgb("#DC2626") : Color.FromArgb("#D6CFC2"),
                    StrokeThickness = 1.2,
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
                    Padding = 14
                };

                var grid = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition(GridLength.Star),
                        new ColumnDefinition(GridLength.Auto)
                    },
                    ColumnSpacing = 10
                };

                var info = new VerticalStackLayout { Spacing = 3 };
                info.Children.Add(new Label
                {
                    Text = item.NombreReceta,
                    FontSize = 15,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#1E293B")
                });
                info.Children.Add(new Label
                {
                    Text = $"{item.CantidadTareas} tareas  •  {item.CantidadDependencias} dependencias  •  {item.TiempoTotalMinutos} min",
                    FontSize = 12,
                    TextColor = Color.FromArgb("#64748B")
                });

                if (item.TieneCiclo)
                {
                    info.Children.Add(new Label
                    {
                        Text = "Alerta DFS: Esta receta contiene un ciclo de dependencias",
                        FontSize = 11,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Color.FromArgb("#DC2626")
                    });
                }

                var btnAbrir = new Button
                {
                    Text = "Llevar a Cocina",
                    BackgroundColor = Color.FromArgb("#E07A5F"),
                    TextColor = Colors.White,
                    FontAttributes = FontAttributes.Bold,
                    FontSize = 12,
                    CornerRadius = 8,
                    Padding = new Thickness(14, 8),
                    VerticalOptions = LayoutOptions.Center
                };

                string rutaCap = item.RutaCompleta;
                btnAbrir.Clicked += (s, e) =>
                {
                    try
                    {
                        _servicio.CargarRecetaDesdeArchivo(rutaCap);
                        NotaSupervision.IsVisible = false;
                        BannerCocina.IsVisible = false;
                        BtnAvanzarChef.IsEnabled = false;
                        BtnCompletarEstacion.IsEnabled = false;
                        _escenarioDrawable.EstacionActiva = null;
                        _escenarioDrawable.EstadoChef = EstadoAnimacionChef.Esperando;
                        _escenarioDrawable.EtiquetaChef = "Listo para iniciar";
                        CambiarVista(1);
                    }
                    catch (Exception ex)
                    {
                        DisplayAlertAsync("Error al cargar", ex.Message, "OK");
                    }
                };

                grid.Add(info, 0, 0);
                grid.Add(btnAbrir, 1, 0);
                card.Content = grid;
                ContenedorCatalogoRecetas.Children.Add(card);
            }
        }

        private void OnAgregarTareaBorradorClicked(object sender, EventArgs e)
        {
            try
            {
                string nombre = TxtNuevaTareaNombre.Text?.Trim() ?? string.Empty;
                string desc = TxtNuevaTareaDesc.Text?.Trim() ?? "Preparacion en estacion";
                if (!int.TryParse(TxtNuevaTareaMinutos.Text, out int min) || min < 0)
                {
                    DisplayAlertAsync("Dato invalido", "Ingresa los minutos estimados (0 o mayor).", "OK");
                    return;
                }

                string id = $"T{_contadorTareasBorrador}";
                Tarea tarea = new Tarea(id, nombre, desc, min);
                _grafoBorrador.AgregarTarea(tarea);
                _contadorTareasBorrador++;

                TxtNuevaTareaNombre.Text = string.Empty;
                TxtNuevaTareaDesc.Text = string.Empty;
                TxtNuevaTareaMinutos.Text = string.Empty;

                RefrescarBorradorUI();
            }
            catch (Exception ex)
            {
                DisplayAlertAsync("Aviso", ex.Message, "OK");
            }
        }

        private void OnAgregarDepBorradorClicked(object sender, EventArgs e)
        {
            try
            {
                if (PickerDepOrigen.SelectedItem is not string orig || PickerDepDestino.SelectedItem is not string dest)
                {
                    DisplayAlertAsync("Selecciona tareas", "Elige la tarea previa y la tarea siguiente.", "OK");
                    return;
                }

                string idOrig = orig.Split(' ')[0].Trim('[', ']');
                string idDest = dest.Split(' ')[0].Trim('[', ']');

                _grafoBorrador.AgregarDependencia(idOrig, idDest);
                RefrescarBorradorUI();
            }
            catch (Exception ex)
            {
                DisplayAlertAsync("Aviso", ex.Message, "OK");
            }
        }

        private void OnLimpiarBorradorClicked(object sender, EventArgs e)
        {
            _grafoBorrador = new GrafoReceta();
            _contadorTareasBorrador = 1;
            TxtNombreNuevaReceta.Text = string.Empty;
            RefrescarBorradorUI();
        }

        private void OnGuardarRecetaBorradorClicked(object sender, EventArgs e)
        {
            try
            {
                string nombre = TxtNombreNuevaReceta.Text?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(nombre) || _grafoBorrador.CantidadVertices == 0)
                {
                    DisplayAlertAsync("Receta incompleta", "Escribe el nombre del plato y agrega al menos una tarea.", "OK");
                    return;
                }

                _servicio.GuardarYActivarReceta(nombre, _grafoBorrador);
                _grafoBorrador = new GrafoReceta();
                _contadorTareasBorrador = 1;
                TxtNombreNuevaReceta.Text = string.Empty;
                RefrescarBorradorUI();
                CargarCatalogoRecetas();

                NotaSupervision.IsVisible = false;
                BannerCocina.IsVisible = false;
                BtnAvanzarChef.IsEnabled = false;
                BtnCompletarEstacion.IsEnabled = false;
                _escenarioDrawable.EstacionActiva = null;
                _escenarioDrawable.EstadoChef = EstadoAnimacionChef.Esperando;
                _escenarioDrawable.EtiquetaChef = "Listo para iniciar";

                CambiarVista(1);
            }
            catch (Exception ex)
            {
                DisplayAlertAsync("Error al guardar", ex.Message, "OK");
            }
        }

        private void RefrescarBorradorUI()
        {
            ListaBorradorTareas.Children.Clear();
            ListaBorradorDeps.Children.Clear();
            PickerDepOrigen.Items.Clear();
            PickerDepDestino.Items.Clear();

            for (int i = 0; i < _grafoBorrador.CantidadVertices; i++)
            {
                Tarea t = _grafoBorrador.ObtenerTareaPorIndice(i);
                TipoEstacionCocina est = ServicioCocinaMaui.ClasificarEstacionDeTarea(t);
                string label = $"[{t.Id}] {t.Nombre}";

                PickerDepOrigen.Items.Add(label);
                PickerDepDestino.Items.Add(label);

                ListaBorradorTareas.Children.Add(new Label
                {
                    Text = $"  • [{t.Id}] {t.Nombre} ({t.TiempoEstimado}m) -> {KitchenSceneDrawable.ObtenerNombreEstacion(est)}",
                    FontSize = 11.5,
                    TextColor = Color.FromArgb("#1E293B")
                });

                ListaSimple<Arista> ady = _grafoBorrador.ObtenerAdyacentes(i);
                NodoLista<Arista>? nodo = ady.cabeza;
                while (nodo != null)
                {
                    ListaBorradorDeps.Children.Add(new Label
                    {
                        Text = $"  ↳ [{nodo.Dato.Origen.Id}] antes que [{nodo.Dato.Destino.Id}]",
                        FontSize = 11.5,
                        TextColor = Color.FromArgb("#E07A5F")
                    });
                    nodo = nodo.Siguiente;
                }
            }
        }

        // =====================================================================
        // VIEW 3: GRAFO INTERACTIVO (SIMULACION VISUAL DE DFS & KAHN)
        // =====================================================================
        private void CargarDatosEnCanvasGrafo()
        {
            var (nodos, aristas) = _servicio.ConstruirGrafoVisual();
            _grafoDrawable.Nodos = nodos;
            _grafoDrawable.Aristas = aristas;
            CanvasGrafo.Invalidate();
        }

        private void OnReiniciarGrafoVisualClicked(object sender, EventArgs e)
        {
            CargarDatosEnCanvasGrafo();
            LblEstadoAlgoritmoGrafo.Text = "Vista del grafo reiniciada.";
            LblResultadoSecuenciaGrafo.Text = "Esperando ejecucion de DFS o Kahn...";
        }

        private async void OnSimularDfsClicked(object sender, EventArgs e)
        {
            if (!_servicio.HayRecetaCargada) return;

            CargarDatosEnCanvasGrafo();
            NodoGrafoVisual[] nodos = _grafoDrawable.Nodos;
            AristaGrafoVisual[] aristas = _grafoDrawable.Aristas;

            LblEstadoAlgoritmoGrafo.Text = "Ejecutando exploracion DFS de 3 estados (Amarillo=NoVisitado, Azul=EnProceso, Verde=Visitado, Rojo=Ciclo)...";
            string traza = "";

            for (int i = 0; i < nodos.Length; i++)
            {
                nodos[i].EstadoAlgoritmo = 1; // Azul: EnProceso
                traza += (traza.Length > 0 ? " -> " : "") + $"Visitar({nodos[i].Id})";
                LblResultadoSecuenciaGrafo.Text = traza;
                CanvasGrafo.Invalidate();
                await Task.Delay(420);

                // Highlight outgoing edges
                for (int a = 0; a < aristas.Length; a++)
                {
                    if (aristas[a].IndiceOrigen == i)
                    {
                        aristas[a].Resaltada = true;
                        if (nodos[aristas[a].IndiceDestino].EstadoAlgoritmo == 1)
                        {
                            aristas[a].EsCiclo = true;
                            nodos[aristas[a].IndiceDestino].EstadoAlgoritmo = 3;
                        }
                    }
                }
                CanvasGrafo.Invalidate();
                await Task.Delay(250);

                if (nodos[i].EstadoAlgoritmo != 3)
                    nodos[i].EstadoAlgoritmo = 2; // Verde: Visitado
            }

            bool tieneCiclo = _servicio.ValidarCicloActual();
            if (tieneCiclo)
            {
                for (int i = 0; i < nodos.Length; i++)
                    if (nodos[i].GradoEntradaActual > 0) nodos[i].EstadoAlgoritmo = 3;

                LblEstadoAlgoritmoGrafo.Text = "DFS finalizado: CICLO DETECTADO en las dependencias. No se puede cocinar.";
                LblResultadoSecuenciaGrafo.Text = "ERROR TOPOLOGICO: Ciclo de dependencias detectado.";
            }
            else
            {
                LblEstadoAlgoritmoGrafo.Text = "DFS finalizado: Grafo Aciclico Dirigido (DAG) valido sin ciclos.";
                LblResultadoSecuenciaGrafo.Text = traza + "  [DAG VALIDO]";
            }
            CanvasGrafo.Invalidate();
        }

        private async void OnSimularKahnClicked(object sender, EventArgs e)
        {
            if (!_servicio.HayRecetaCargada) return;

            if (_servicio.ValidarCicloActual())
            {
                await OnSimularDfsReturnWarning();
                return;
            }

            CargarDatosEnCanvasGrafo();
            NodoGrafoVisual[] nodos = _grafoDrawable.Nodos;
            AristaGrafoVisual[] aristas = _grafoDrawable.Aristas;

            int n = nodos.Length;
            bool[] procesado = new bool[n];
            string secuencia = "";

            LblEstadoAlgoritmoGrafo.Text = "Ejecutando Ordenamiento Topologico de Kahn: seleccionando tareas con Grado de Entrada = 0...";

            for (int paso = 0; paso < n; paso++)
            {
                int elegido = -1;
                for (int i = 0; i < n; i++)
                {
                    if (!procesado[i] && nodos[i].GradoEntradaActual == 0)
                    {
                        elegido = i;
                        break;
                    }
                }

                if (elegido == -1) break;

                // Highlight selected degree-0 node in Blue
                nodos[elegido].EstadoAlgoritmo = 1;
                LblEstadoAlgoritmoGrafo.Text = $"Kahn Paso {paso + 1}: [{nodos[elegido].Id}] tiene Grado de Entrada 0 -> entra al plan.";
                CanvasGrafo.Invalidate();
                await Task.Delay(500);

                // Reduce in-degrees of dependent nodes
                for (int a = 0; a < aristas.Length; a++)
                {
                    if (aristas[a].IndiceOrigen == elegido)
                    {
                        aristas[a].Resaltada = true;
                        int dest = aristas[a].IndiceDestino;
                        if (nodos[dest].GradoEntradaActual > 0)
                            nodos[dest].GradoEntradaActual--;
                    }
                }

                procesado[elegido] = true;
                nodos[elegido].EstadoAlgoritmo = 2; // Green: Added to topological order
                secuencia += (secuencia.Length > 0 ? " -> " : "") + $"[{nodos[elegido].Id}] {nodos[elegido].Nombre}";
                LblResultadoSecuenciaGrafo.Text = secuencia;
                CanvasGrafo.Invalidate();
                await Task.Delay(350);
            }

            LblEstadoAlgoritmoGrafo.Text = "Ordenamiento de Kahn completado. Listo para ejecutar en Cocina en Vivo.";
        }

        private Task OnSimularDfsReturnWarning()
        {
            LblEstadoAlgoritmoGrafo.Text = "No se puede ejecutar Kahn porque el grafo contiene un ciclo (ejecuta Simular DFS para verlo).";
            LblResultadoSecuenciaGrafo.Text = "BLOQUEADO POR CICLO";
            return Task.CompletedTask;
        }
    }
}
