using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace WebApplication1.Data.Exportacion
{
    public static class PdfExportador
    {
        private const string Teal = "#17A6A0";
        private const string TealOscuro = "#128F8A";
        private const string Zebra = "#F4F8F8";
        private const string Linea = "#D9E3E3";
        private const string GrisTexto = "#6B7280";

        // ---------- REPORTE (tablas) ----------

        public static byte[] GenerarReporte(LibroReporte libro, byte[]? logo)
        {
            return Document.Create(documento =>
            {
                documento.Page(pagina =>
                {
                    pagina.Size(PageSizes.A4.Landscape());
                    pagina.Margin(28);
                    pagina.DefaultTextStyle(x => x.FontSize(8.5f).FontColor("#1F2937"));

                    pagina.Header().Element(c => Cabecera(c, libro, logo));
                    pagina.Content().PaddingVertical(10).Element(c => Contenido(c, libro));
                    pagina.Footer().Element(c => Pie(c, libro));
                });
            }).GeneratePdf();
        }

        private static void Cabecera(IContainer contenedor, LibroReporte libro, byte[]? logo)
        {
            contenedor.Column(col =>
            {
                col.Item().Row(fila =>
                {
                    if (logo is { Length: > 0 })
                        fila.ConstantItem(56).Height(40).Image(logo).FitArea();

                    fila.RelativeItem().PaddingLeft(logo is null ? 0 : 10).Column(c =>
                    {
                        c.Item().Text(libro.Titulo).FontSize(17).Bold().FontColor(Teal);
                        if (!string.IsNullOrWhiteSpace(libro.Subtitulo))
                            c.Item().Text(libro.Subtitulo).FontColor(GrisTexto);
                    });

                    fila.ConstantItem(170).AlignRight().Column(c =>
                    {
                        c.Item().AlignRight().Text("CHASKI-RUTA").Bold().FontColor(TealOscuro);
                        c.Item().AlignRight().Text(FormatoEs.FechaHora(libro.Generado)).FontColor(GrisTexto);
                    });
                });
                col.Item().PaddingTop(6).LineHorizontal(1.5f).LineColor(Teal);
            });
        }

        private static void Contenido(IContainer contenedor, LibroReporte libro)
        {
            contenedor.Column(col =>
            {
                col.Spacing(10);

                if (libro.Filtros.Count > 0)
                {
                    col.Item().Text(t =>
                    {
                        t.Span("Filtros: ").Bold();
                        t.Span(string.Join("   ·   ", libro.Filtros.Select(f => $"{f.Etiqueta}: {f.Valor}"))).FontColor(GrisTexto);
                    });
                }

                if (libro.Resumen.Count > 0)
                {
                    col.Item().Row(fila =>
                    {
                        fila.Spacing(8);
                        foreach (var (etiqueta, valor) in libro.Resumen)
                        {
                            fila.RelativeItem().Border(0.7f).BorderColor(Linea).Background(Zebra).Padding(8).Column(c =>
                            {
                                c.Item().Text(etiqueta.ToUpperInvariant()).FontSize(7.5f).FontColor(GrisTexto);
                                c.Item().Text(valor).FontSize(14).Bold();
                            });
                        }
                    });
                }

                foreach (var seccion in libro.Secciones)
                {
                    // Una tabla corta se mantiene completa en una página; las largas sí pueden continuar en la siguiente
                    var item = col.Item();
                    if (seccion.Filas.Count <= 14) item = item.ShowEntire();
                    item.Element(c => Tabla(c, seccion));
                }
            });
        }

        private static void Tabla(IContainer contenedor, SeccionTabla seccion)
        {
            contenedor.Column(col =>
            {
                col.Spacing(4);
                if (!string.IsNullOrWhiteSpace(seccion.Titulo))
                    col.Item().Text(seccion.Titulo.ToUpperInvariant()).FontSize(10).Bold().FontColor(TealOscuro);

                if (seccion.Filas.Count == 0)
                {
                    col.Item().Border(0.7f).BorderColor(Linea).Padding(10)
                        .Text("No hay datos para los filtros elegidos.").FontColor(GrisTexto).Italic();
                    return;
                }

                var cols = seccion.Columnas;
                // Para las barras: el mayor valor de cada columna
                var maximos = cols.Select((c, i) => FormatoEs.EsBarra(c.Tipo)
                    ? seccion.Filas.Select(f => Convertir(i < f.Length ? f[i] : null)).DefaultIfEmpty(0).Max()
                    : 0m).ToArray();

                col.Item().Table(tabla =>
                {
                    tabla.ColumnsDefinition(d => { foreach (var c in cols) d.RelativeColumn(c.Peso); });

                    tabla.Header(h =>
                    {
                        foreach (var c in cols)
                        {
                            var celda = h.Cell().Background(Teal).PaddingVertical(5).PaddingHorizontal(5);
                            if (FormatoEs.EsNumerico(c.Tipo)) celda.AlignRight().Text(c.Titulo).Bold().FontColor(Colors.White);
                            else celda.Text(c.Titulo).Bold().FontColor(Colors.White);
                        }
                    });

                    for (var f = 0; f < seccion.Filas.Count; f++)
                    {
                        var fila = seccion.Filas[f];
                        var fondo = f % 2 == 1 ? Color.FromHex(Zebra) : Colors.White;
                        for (var c = 0; c < cols.Count; c++)
                            Celda(tabla.Cell().Background(fondo).BorderBottom(0.4f).BorderColor(Linea).PaddingVertical(3.5f).PaddingHorizontal(5),
                                  c < fila.Length ? fila[c] : null, cols[c], maximos[c], negrita: false);
                    }

                    if (seccion.Totales is { } totales)
                    {
                        for (var c = 0; c < cols.Count; c++)
                            Celda(tabla.Cell().Background("#E7F4F3").BorderTop(1).BorderColor(Teal).PaddingVertical(4.5f).PaddingHorizontal(5),
                                  c < totales.Length ? totales[c] : null, cols[c], 0, negrita: true);
                    }
                });
            });
        }

        private static void Celda(IContainer celda, object? valor, ColumnaTabla columna, decimal maximo, bool negrita)
        {
            var texto = FormatoEs.Texto(valor, columna.Tipo);

            if (FormatoEs.EsBarra(columna.Tipo) && maximo > 0 && Convertir(valor) is var v)
            {
                var pct = (float)Math.Clamp(v / maximo * 100m, 0m, 100m);
                // ShowEntire: la barra nunca se separa de su número en otra página
                celda.ShowEntire().Column(c =>
                {
                    c.Item().AlignRight().Text(texto);
                    c.Item().PaddingTop(2).Height(4).Row(r =>
                    {
                        r.RelativeItem(Math.Max(pct, 0.5f)).Background(Teal);
                        r.RelativeItem(Math.Max(100f - pct, 0.01f)).Background("#E3ECEC");
                    });
                });
                return;
            }

            var t = FormatoEs.EsNumerico(columna.Tipo) ? celda.AlignRight().Text(texto) : celda.Text(texto);
            if (negrita) t.Bold();
        }

        private static decimal Convertir(object? v) => v switch
        {
            decimal d => d,
            double db => (decimal)db,
            int i => i,
            long l => l,
            _ => 0m
        };

        private static void Pie(IContainer contenedor, LibroReporte libro)
        {
            contenedor.Column(col =>
            {
                col.Item().LineHorizontal(0.5f).LineColor(Linea);
                col.Item().PaddingTop(4).Row(fila =>
                {
                    fila.RelativeItem().Text($"Generado por {libro.GeneradoPor} · Sistema Chaski-Ruta").FontSize(8).FontColor(GrisTexto);
                    fila.ConstantItem(100).AlignRight().Text(t =>
                    {
                        t.DefaultTextStyle(x => x.FontSize(8).FontColor(GrisTexto));
                        t.Span("Página ");
                        t.CurrentPageNumber();
                        t.Span(" de ");
                        t.TotalPages();
                    });
                });
            });
        }
    }
}
