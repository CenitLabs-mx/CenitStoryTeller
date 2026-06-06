using System;
using System.Collections.Generic;

namespace CenitStoryTeller.Core.Entities;

// ---------- Enums ----------
public enum IntakeTipo { Idea, Bosquejo, Borrador, DominioPublico }
public enum ObraEstado { Idea, Biblia, Borrador, Edicion, Publicada }
public enum CanonNivel { Canonico, Borrador, Observado }
public enum EstadoVital { Vivo, Muerto, Indefinido }
public enum Acto { Backstory, Setup, Acto1, Acto2, Acto3, Epilogo }
public enum FuncionNarrativa { Setup, Detonante, Giro, PuntoMedio, Crisis, Climax, Resolucion }
public enum BeatEstado { Pendiente, Escrito, Validado }
public enum EventoTipo { Canonico, Borrador, Backstory }
public enum CapituloEstado { Esquema, Borrador, Edicion, Publicado }
public enum Veredicto { Aprobado, Revisar, Rechazado }

// ---------- Núcleo ----------
public class Obra
{
    public Guid Id { get; set; }
    public string Titulo { get; set; } = "";
    public string? Medio { get; set; }
    public string? Genero { get; set; }
    public string? Logline { get; set; }
    public IntakeTipo Intake { get; set; }
    public ObraEstado Estado { get; set; }
    public string? PlataformaObjetivo { get; set; }
    public string? UniversoCompartido { get; set; }
    public string? TemaCentral { get; set; }

    public List<Personaje> Personajes { get; set; } = new();
    public List<Ubicacion> Ubicaciones { get; set; } = new();
    public List<Beat> Beats { get; set; } = new();
    public List<Evento> Eventos { get; set; } = new();
    public List<Capitulo> Capitulos { get; set; } = new();
}

public class Personaje
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = "";
    public string? Rol { get; set; }
    public string? Arquetipo { get; set; }
    public string? HeridaCentral { get; set; }
    public string? Deseo { get; set; }
    public string? Necesidad { get; set; }
    public string? Restricciones { get; set; }   // lo que su cuerpo/psique no soporta
    public string? EstadoEnTrama { get; set; }
    public EstadoVital EstadoVital { get; set; }
    public CanonNivel Canon { get; set; }
    public Guid ObraId { get; set; }
}

public class Ubicacion
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = "";
    public string? Tipo { get; set; }
    public string? RolEnTrama { get; set; }
    public string? EstadoActual { get; set; }
    public CanonNivel Canon { get; set; }
    public Guid ObraId { get; set; }
}

public class Beat
{
    public Guid Id { get; set; }
    public string Titulo { get; set; } = "";
    public int Orden { get; set; }
    public Acto Acto { get; set; }
    public FuncionNarrativa Funcion { get; set; }
    public string? Descripcion { get; set; }
    public BeatEstado Estado { get; set; }
    public Guid ObraId { get; set; }
}

public class Evento
{
    public Guid Id { get; set; }
    public string Titulo { get; set; } = "";
    public int Orden { get; set; }
    public Acto Acto { get; set; }
    public EventoTipo Tipo { get; set; }
    public string? MomentoInWorld { get; set; }
    public string? CambioConsecuencia { get; set; }
    public Guid ObraId { get; set; }
    public List<Personaje> Personajes { get; set; } = new();   // N:N
    public List<Ubicacion> Ubicaciones { get; set; } = new();  // N:N
}

public class Capitulo
{
    public Guid Id { get; set; }
    public string Titulo { get; set; } = "";
    public int Orden { get; set; }
    public CapituloEstado Estado { get; set; }
    public string? Plataforma { get; set; }
    public Guid? BeatObjetivoId { get; set; }
    public Guid ObraId { get; set; }
    public List<CapituloVersion> Versiones { get; set; } = new();
}

// ---------- Trazabilidad (lo que alimenta el dashboard) ----------
public class CapituloVersion
{
    public Guid Id { get; set; }
    public Guid CapituloId { get; set; }
    public Capitulo? Capitulo { get; set; }
    public int NumeroVersion { get; set; }      // v1, v2, ... para comparativa
    public string Modelo { get; set; } = "";     // ej. gpt-4o-mini, gemini-1.5, gemma-local
    public string PromptUsado { get; set; } = "";
    public string Texto { get; set; } = "";
    public bool EsFinal { get; set; }
    public DateTimeOffset CreadoEn { get; set; }
    public PruebaAcido? Acido { get; set; }
}

public class PruebaAcido
{
    public Guid Id { get; set; }
    public Guid CapituloVersionId { get; set; }
    public bool Fisica { get; set; }
    public bool Psicologica { get; set; }
    public bool Ambiental { get; set; }
    public bool Quimica { get; set; }
    public bool Anacronismo { get; set; }
    public bool FidelidadFuncional { get; set; }
    public Veredicto Veredicto { get; set; }
    public string? Hallazgos { get; set; }
    public string? Parches { get; set; }
}

public class RegistroPaso   // log auditable: "cada paso de cada proyecto"
{
    public Guid Id { get; set; }
    public Guid ObraId { get; set; }
    public string Agente { get; set; } = "";
    public string Accion { get; set; } = "";
    public string? CanonSnapshot { get; set; }   // qué canon vio el agente (JSON)
    public string? Cambios { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
