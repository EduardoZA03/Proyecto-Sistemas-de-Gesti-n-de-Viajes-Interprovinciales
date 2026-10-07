using ClosedXML.Excel;

namespace WebApplication1.Data.Exportacion
{
    public static class ExcelExportador
    {
        private const string Teal = "#17A6A0";
        private const string Gris = "#F4F8F8";

        public static byte[] Generar(LibroReporte libro)
        {
            using var libroXl = new XLWorkbook();
            libroXl.Properties.Title = libro.Titulo;
            libroXl.Properties.Author = "Chaski-Ruta";

            HojaResumen(libroXl, libro);

            var usados = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Resumen" };
            foreach (var seccion in libro.Secciones)
                HojaDatos(libroXl, seccion, NombreHoja(seccion.Titulo, usados));

            // La primera hoja que se ve es la de datos si solo hay una; si no, el resumen
            using var ms = new MemoryStream();
            libroXl.SaveAs(ms);
            return ms.ToArray();
        }

        private static void HojaResumen(XLWorkbook libroXl, LibroReporte libro)
        {
            var ws = libroXl.AddWorksheet("Resumen");
            ws.ShowGridLines = false;
            ws.Column(1).Width = 26;
            ws.Column(2).Width = 48;

            var fila = 1;
            ws.Cell(fila, 1).SetValue(libro.Titulo).Style.Font.SetBold().Font.SetFontSize(16).Font.SetFontColor(XLColor.FromHtml(Teal));
            fila++;
            if (!string.IsNullOrWhiteSpace(libro.Subtitulo))
            {
                ws.Cell(fila, 1).SetValue(libro.Subtitulo).Style.Font.SetFontColor(XLColor.Gray);
                fila++;
            }
            fila++;

            if (libro.Filtros.Count > 0)
            {
                ws.Cell(fila, 1).SetValue("Filtros aplicados").Style.Font.SetBold();
                fila++;
                foreach (var (etiqueta, valor) in libro.Filtros)
                {
                    ws.Cell(fila, 1).SetValue(etiqueta);
                    ws.Cell(fila, 2).SetValue(valor);
                    fila++;
                }
                fila++;
            }

            if (libro.Resumen.Count > 0)
            {
                ws.Cell(fila, 1).SetValue("Resumen").Style.Font.SetBold();
                fila++;
                foreach (var (etiqueta, valor) in libro.Resumen)
                {
                    ws.Cell(fila, 1).SetValue(etiqueta);
                    ws.Cell(fila, 2).SetValue(valor).Style.Font.SetBold();
                    ws.Cell(fila, 2).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);
                    fila++;
                }
                fila++;
            }

            ws.Cell(fila, 1).SetValue("Generado por");
            ws.Cell(fila, 2).SetValue(libro.GeneradoPor);
            fila++;
            ws.Cell(fila, 1).SetValue("Fecha de generación");
            ws.Cell(fila, 2).SetValue(FormatoEs.FechaHora(libro.Generado));
        }

        private static void HojaDatos(XLWorkbook libroXl, SeccionTabla seccion, string nombre)
        {
            var ws = libroXl.AddWorksheet(nombre);
            var cols = seccion.Columnas;

            // Encabezado (fila 1, para poder filtrar y usar tablas dinámicas)
            for (var c = 0; c < cols.Count; c++)
            {
                var celda = ws.Cell(1, c + 1);
                celda.SetValue(cols[c].Titulo);
                celda.Style.Font.SetBold().Font.SetFontColor(XLColor.White);
                celda.Style.Fill.SetBackgroundColor(XLColor.FromHtml(Teal));
                celda.Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);
                if (FormatoEs.EsNumerico(cols[c].Tipo))
                    celda.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
            }
            ws.Row(1).Height = 22;

            // Datos
            for (var f = 0; f < seccion.Filas.Count; f++)
            {
                var fila = seccion.Filas[f];
                for (var c = 0; c < cols.Count; c++)
                    Poner(ws.Cell(f + 2, c + 1), c < fila.Length ? fila[c] : null, cols[c].Tipo);
                if (f % 2 == 1)
                    ws.Range(f + 2, 1, f + 2, cols.Count).Style.Fill.SetBackgroundColor(XLColor.FromHtml(Gris));
            }

            var ultimaFila = seccion.Filas.Count + 1;

            // Totales
            if (seccion.Totales is { } totales)
            {
                ultimaFila++;
                for (var c = 0; c < cols.Count; c++)
                    Poner(ws.Cell(ultimaFila, c + 1), c < totales.Length ? totales[c] : null, cols[c].Tipo);
                var rangoTotal = ws.Range(ultimaFila, 1, ultimaFila, cols.Count);
                rangoTotal.Style.Font.SetBold();
                rangoTotal.Style.Border.SetTopBorder(XLBorderStyleValues.Medium);
                rangoTotal.Style.Border.SetTopBorderColor(XLColor.FromHtml(Teal));
            }

            // Filtro automático, encabezado fijo y anchos
            if (seccion.Filas.Count > 0)
                ws.Range(1, 1, seccion.Filas.Count + 1, cols.Count).SetAutoFilter();
            ws.SheetView.FreezeRows(1);
            ws.Columns().AdjustToContents(1, Math.Min(ultimaFila, 200));
            foreach (var columna in ws.ColumnsUsed())
                columna.Width = Math.Clamp(columna.Width + 2, 10, 60);
        }

        // Cada valor se escribe con su tipo real (fecha, número, texto): así Excel puede ordenar y sumar.
        // Los textos se guardan siempre como texto, nunca como fórmula (aunque empiecen con "=").
        private static void Poner(IXLCell celda, object? valor, TipoColumna tipo)
        {
            switch (valor)
            {
                case null:
                    return;
                case DateTime d:
                    celda.SetValue(d);
                    celda.Style.DateFormat.Format = tipo == TipoColumna.FechaHora ? "dd/mm/yyyy hh:mm" : "dd/mm/yyyy";
                    celda.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);
                    return;
                case decimal m:
                    celda.SetValue(m);
                    celda.Style.NumberFormat.Format = FormatoNumero(tipo);
                    return;
                case double db:
                    celda.SetValue(db);
                    celda.Style.NumberFormat.Format = FormatoNumero(tipo);
                    return;
                case int n:
                    celda.SetValue(n);
                    celda.Style.NumberFormat.Format = tipo == TipoColumna.Moneda ? FormatoNumero(tipo) : "#,##0";
                    return;
                case long l:
                    celda.SetValue(l);
                    celda.Style.NumberFormat.Format = "#,##0";
                    return;
                default:
                    celda.SetValue(valor.ToString() ?? string.Empty);
                    return;
            }
        }

        private static string FormatoNumero(TipoColumna tipo) => tipo switch
        {
            TipoColumna.Moneda or TipoColumna.BarraMoneda => "\"S/\" #,##0.00",
            TipoColumna.Entero => "#,##0",
            TipoColumna.Porcentaje => "0.0\"%\"",
            _ => "#,##0.00"
        };

        // Excel limita el nombre de hoja a 31 caracteres, sin  []:*?/\  y sin repetirse
        private static string NombreHoja(string titulo, HashSet<string> usados)
        {
            var limpio = new string(titulo.Where(ch => !"[]:*?/\\".Contains(ch)).ToArray()).Trim();
            if (limpio == "") limpio = "Datos";
            if (limpio.Length > 31) limpio = limpio[..31];
            var nombre = limpio;
            var n = 2;
            while (!usados.Add(nombre))
            {
                var sufijo = $" ({n++})";
                nombre = limpio[..Math.Min(limpio.Length, 31 - sufijo.Length)] + sufijo;
            }
            return nombre;
        }
    }
}
