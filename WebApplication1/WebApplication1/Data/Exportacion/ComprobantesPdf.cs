using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace WebApplication1.Data.Exportacion
{
    public record DatosEmpresa(string RazonSocial, string Ruc, string Direccion, string Telefono, string Correo);

    public record DatosComprobanteVenta(
        string Codigo, DateTime FechaPago, string Estado,
        string CodigoReserva, string Pasajero, string TipoDocumento, string Documento,
        string Ruta, DateTime Salida, string Servicio, string Placa,
        List<(string Asiento, string Tarifa, decimal Precio)> Asientos,
        decimal Monto, string MetodoPago, string Referencia,
        string AtendidoPor, DatosEmpresa Empresa);

    public record DatosComprobanteCancelacion(
        string Codigo, DateTime FechaCancelacion, string Estado, string Motivo,
        string CodigoReserva, string Pasajero, string TipoDocumento, string Documento,
        string Ruta, DateTime Salida, string Servicio,
        decimal TotalReserva, decimal Pagado, decimal Devolucion, decimal Penalidad, int PorcentajeDevuelto,
        DatosEmpresa Empresa);

    public static class ComprobantesPdf
    {
        private const string Teal = "#17A6A0";
        private const string TealOscuro = "#128F8A";
        private const string Zebra = "#F4F8F8";
        private const string Linea = "#D9E3E3";
        private const string GrisTexto = "#6B7280";
        private const string Rojo = "#B42318";

        private const string AvisoLegal =
            "Documento interno de control del sistema Chaski-Ruta. No es un comprobante de pago electrónico autorizado por la SUNAT.";

        // ---------- VENTA ----------

        public static byte[] Venta(DatosComprobanteVenta d, byte[]? logo)
        {
            return Document.Create(documento => documento.Page(pagina =>
            {
                Configurar(pagina);
                pagina.Header().Element(c => Cabecera(c, d.Empresa, logo, "COMPROBANTE DE VENTA", d.Codigo, FormatoEs.FechaHora(d.FechaPago)));
                pagina.Content().PaddingVertical(12).Column(col =>
                {
                    col.Spacing(10);

                    col.Item().Element(c => Bloque(c, "Pasajero", b =>
                    {
                        Dato(b, "Nombre", d.Pasajero);
                        Dato(b, d.TipoDocumento, d.Documento);
                    }));

                    col.Item().Element(c => Bloque(c, "Viaje", b =>
                    {
                        Dato(b, "Ruta", d.Ruta);
                        Dato(b, "Salida", FormatoEs.FechaHora(d.Salida));
                        Dato(b, "Servicio", $"{d.Servicio} · Bus {d.Placa}");
                        Dato(b, "Reserva", d.CodigoReserva);
                    }));

                    col.Item().Element(c => Bloque(c, "Detalle", b =>
                    {
                        b.Item().Table(t =>
                        {
                            t.ColumnsDefinition(x => { x.RelativeColumn(1); x.RelativeColumn(2); x.RelativeColumn(1.4f); });
                            t.Header(h =>
                            {
                                h.Cell().BorderBottom(1).BorderColor(Teal).PaddingBottom(3).Text("Asiento").Bold().FontColor(TealOscuro);
                                h.Cell().BorderBottom(1).BorderColor(Teal).PaddingBottom(3).Text("Tarifa").Bold().FontColor(TealOscuro);
                                h.Cell().BorderBottom(1).BorderColor(Teal).PaddingBottom(3).AlignRight().Text("Precio").Bold().FontColor(TealOscuro);
                            });
                            foreach (var (asiento, tarifa, precio) in d.Asientos)
                            {
                                t.Cell().PaddingVertical(3).Text(asiento);
                                t.Cell().PaddingVertical(3).Text(tarifa);
                                t.Cell().PaddingVertical(3).AlignRight().Text(FormatoEs.Moneda(precio));
                            }
                        });
                    }));

                    col.Item().Background(Zebra).Border(0.8f).BorderColor(Linea).Padding(10).Column(b =>
                    {
                        b.Item().Row(r =>
                        {
                            r.RelativeItem().Text("TOTAL PAGADO").Bold().FontColor(GrisTexto);
                            r.RelativeItem().AlignRight().Text(FormatoEs.Moneda(d.Monto)).FontSize(18).Bold().FontColor(TealOscuro);
                        });
                        b.Item().PaddingTop(4).Text($"Método de pago: {d.MetodoPago}" +
                            (string.IsNullOrWhiteSpace(d.Referencia) ? "" : $"  ·  Ref.: {d.Referencia}") +
                            $"  ·  Estado: {d.Estado}").FontColor(GrisTexto);
                    });

                    col.Item().Text($"Atendido por: {d.AtendidoPor}").FontColor(GrisTexto);
                });
                pagina.Footer().Element(c => Pie(c));
            })).GeneratePdf();
        }

        // ---------- CANCELACIÓN ----------

        public static byte[] Cancelacion(DatosComprobanteCancelacion d, byte[]? logo)
        {
            return Document.Create(documento => documento.Page(pagina =>
            {
                Configurar(pagina);
                pagina.Header().Element(c => Cabecera(c, d.Empresa, logo, "CONSTANCIA DE CANCELACIÓN", d.Codigo, FormatoEs.FechaHora(d.FechaCancelacion)));
                pagina.Content().PaddingVertical(12).Column(col =>
                {
                    col.Spacing(10);

                    col.Item().Element(c => Bloque(c, "Pasajero", b =>
                    {
                        Dato(b, "Nombre", d.Pasajero);
                        Dato(b, d.TipoDocumento, d.Documento);
                    }));

                    col.Item().Element(c => Bloque(c, "Reserva cancelada", b =>
                    {
                        Dato(b, "Reserva", d.CodigoReserva);
                        Dato(b, "Ruta", d.Ruta);
                        Dato(b, "Salida", FormatoEs.FechaHora(d.Salida));
                        Dato(b, "Servicio", d.Servicio);
                        Dato(b, "Motivo", string.IsNullOrWhiteSpace(d.Motivo) ? "—" : d.Motivo);
                    }));

                    col.Item().Element(c => Bloque(c, "Liquidación", b =>
                    {
                        Fila(b, "Total de la reserva", FormatoEs.Moneda(d.TotalReserva));
                        Fila(b, "Monto pagado", FormatoEs.Moneda(d.Pagado));
                        Fila(b, $"Penalidad ({100 - d.PorcentajeDevuelto} %)", "− " + FormatoEs.Moneda(d.Penalidad), Rojo);
                    }));

                    col.Item().Background(Zebra).Border(0.8f).BorderColor(Linea).Padding(10).Column(b =>
                    {
                        b.Item().Row(r =>
                        {
                            r.RelativeItem().Text("DEVOLUCIÓN").Bold().FontColor(GrisTexto);
                            r.RelativeItem().AlignRight().Text(FormatoEs.Moneda(d.Devolucion)).FontSize(18).Bold().FontColor(TealOscuro);
                        });
                        b.Item().PaddingTop(4).Text($"Estado: {d.Estado}" + (d.Pagado > 0 ? $"  ·  Se devuelve el {d.PorcentajeDevuelto} % de lo pagado" : "  ·  La reserva no tenía pagos")).FontColor(GrisTexto);
                    });
                });
                pagina.Footer().Element(c => Pie(c));
            })).GeneratePdf();
        }

        // ---------- piezas comunes ----------

        private static void Configurar(PageDescriptor pagina)
        {
            pagina.Size(PageSizes.A5);
            pagina.Margin(24);
            pagina.DefaultTextStyle(x => x.FontSize(10).FontColor("#1F2937"));
        }

        private static void Cabecera(IContainer contenedor, DatosEmpresa empresa, byte[]? logo, string titulo, string codigo, string fecha)
        {
            contenedor.Column(col =>
            {
                col.Item().Row(fila =>
                {
                    if (logo is { Length: > 0 })
                        fila.ConstantItem(50).Height(36).Image(logo).FitArea();

                    fila.RelativeItem().PaddingLeft(logo is null ? 0 : 8).Column(c =>
                    {
                        c.Item().Text(empresa.RazonSocial).Bold().FontSize(11).FontColor(TealOscuro);
                        c.Item().Text($"RUC {empresa.Ruc}").FontSize(8.5f).FontColor(GrisTexto);
                        if (!string.IsNullOrWhiteSpace(empresa.Direccion)) c.Item().Text(empresa.Direccion).FontSize(8.5f).FontColor(GrisTexto);
                        var contacto = string.Join(" · ", new[] { empresa.Telefono, empresa.Correo }.Where(x => !string.IsNullOrWhiteSpace(x)));
                        if (contacto != "") c.Item().Text(contacto).FontSize(8.5f).FontColor(GrisTexto);
                    });
                });

                col.Item().PaddingTop(8).Background(Teal).Padding(8).Row(r =>
                {
                    r.RelativeItem().Text(titulo).Bold().FontSize(11).FontColor(Colors.White);
                    r.AutoItem().AlignRight().Text(codigo).Bold().FontSize(11).FontColor(Colors.White);
                });
                col.Item().PaddingTop(3).AlignRight().Text($"Emitido: {fecha}").FontSize(8.5f).FontColor(GrisTexto);
            });
        }

        private static void Bloque(IContainer contenedor, string titulo, Action<ColumnDescriptor> contenido)
        {
            contenedor.Border(0.8f).BorderColor(Linea).Padding(8).Column(col =>
            {
                col.Spacing(3);
                col.Item().Text(titulo.ToUpperInvariant()).FontSize(8).Bold().FontColor(GrisTexto);
                contenido(col);
            });
        }

        private static void Dato(ColumnDescriptor col, string etiqueta, string valor)
        {
            col.Item().Row(r =>
            {
                r.ConstantItem(70).Text(etiqueta).FontColor(GrisTexto);
                r.RelativeItem().Text(valor).SemiBold();
            });
        }

        private static void Fila(ColumnDescriptor col, string etiqueta, string valor, string? color = null)
        {
            col.Item().Row(r =>
            {
                r.RelativeItem().Text(etiqueta);
                var t = r.ConstantItem(110).AlignRight().Text(valor);
                if (color != null) t.FontColor(color);
            });
        }

        private static void Pie(IContainer contenedor)
        {
            contenedor.Column(col =>
            {
                col.Item().LineHorizontal(0.5f).LineColor(Linea);
                col.Item().PaddingTop(4).Text(AvisoLegal).FontSize(7.5f).FontColor(GrisTexto).AlignCenter();
            });
        }
    }
}
