using System;
using System.Collections.Generic;

namespace TorneoPro.API.Models;

public partial class multas_historial_pago
{
    public int id { get; set; }

    public string codigo { get; set; } = null!;

    public int id_multa { get; set; }

    public int id_metodo_pago { get; set; }

    public decimal monto_pagado { get; set; }

    public string? moneda { get; set; }

    public DateTime? fecha_pago { get; set; }

    public string? codigo_transaccion { get; set; }

    public string? comprobante_url { get; set; }

    public bool? comprobante_verificado { get; set; }

    public DateTime? fecha_verificacion { get; set; }

    public int? id_usuario_verificacion { get; set; }

    public string? estado { get; set; }

    public string? motivo_rechazo { get; set; }

    public int? id_usuario_pago { get; set; }

    public string? metadata { get; set; }

    public virtual metodos_pago id_metodo_pagoNavigation { get; set; } = null!;

    public virtual multa id_multaNavigation { get; set; } = null!;

    public virtual usuario? id_usuario_pagoNavigation { get; set; }

    public virtual usuario? id_usuario_verificacionNavigation { get; set; }
}
