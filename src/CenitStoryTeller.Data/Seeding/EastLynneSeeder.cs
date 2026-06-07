using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CenitStoryTeller.Core.Entities;

namespace CenitStoryTeller.Data.Seeding;

// Carga el ejemplo canónico de East Lynne (dominio público) para tener datos
// reales con los que probar el índice, la bitácora y la Prueba del Ácido.
public static class EastLynneSeeder
{
    public const string TituloObra = "East Lynne";

    public static async Task<bool> SeedAsync(CenitStoryTellerDbContext db, CancellationToken ct = default)
    {
        if (await db.Obras.AnyAsync(o => o.Titulo == TituloObra, ct))
            return false;

        var obra = new Obra
        {
            Id = Guid.NewGuid(),
            Titulo = TituloObra,
            Medio = "Novela serializada",
            Genero = "Melodrama / Romance trágico",
            Logline = "Una heredera que lo pierde todo se casa por refugio y, arrastrada por " +
                      "un viejo encanto, comete el único error que no se puede deshacer.",
            Intake = IntakeTipo.DominioPublico,
            Estado = ObraEstado.Biblia,
            PlataformaObjetivo = "Wattpad",
            TemaCentral = "El precio irreversible de un solo impulso."
        };

        // ----- Personajes -----
        var isabel = NuevoPersonaje(obra, "Isabel Vane", "Protagonista",
            "La heredera inocente que cae",
            "Criada en una burbuja de lujo sin herramientas para el mundo real; carencia afectiva del padre tras la fachada.",
            "Ser amada y pertenecer a algún lugar seguro.",
            "Aprender a sostenerse sola y asumir las consecuencias de sus propias decisiones.",
            "Inicia protegida; pierde todo, se casa con Carlyle y comete el error irreversible.",
            EstadoVital.Vivo);

        var carlyle = NuevoPersonaje(obra, "Archie Carlyle", "Deuteragonista",
            "El hombre hecho a sí mismo / el ancla",
            "Viene de abajo; miedo crónico a no ser 'suficiente' para el mundo del viejo dinero.",
            "Estabilidad, respeto y una familia sólida.",
            "Aprender a expresar afecto y no solo a proveer y proteger.",
            "Compra East Lynne, se casa con Isabel; tras el abandono rehace su vida.",
            EstadoVital.Vivo);

        var william = NuevoPersonaje(obra, "William Vane", "Secundario",
            "El patriarca trágico y disipado",
            "Heredó una fortuna que nunca sintió merecer y jamás se perdonó haberla dilapidado.",
            "Mantener la fachada de gran señor hasta el final.",
            "Aceptar la ruina y reparar el vínculo con su hija antes de morir.",
            "Arruinado y enfermo al inicio; muere pronto, dejando a Isabel sin nada.",
            EstadoVital.Muerto);

        var levison = NuevoPersonaje(obra, "Francis Levison", "Antagonista",
            "El seductor encantador / villano carismático",
            "Vacío narcisista: solo se siente vivo conquistando lo ajeno.",
            "Placer y conquista sin pagar ningún costo.",
            "Enfrentar por fin las consecuencias de sus actos.",
            "Seduce a Isabel y la convence de abandonar a su familia; luego la descarta.",
            EstadoVital.Vivo);

        var barbara = NuevoPersonaje(obra, "Barbara Hare", "Deuteragonista",
            "El amor leal / 'la otra' que siempre estuvo ahí",
            "Amar durante años a alguien que ama (o eligió) a otra.",
            "Que Carlyle por fin la vea y la elija.",
            "Ser amada por sí misma, no como segundo premio.",
            "Amor no correspondido de Carlyle; tras el abandono de Isabel, ocupa su lugar.",
            EstadoVital.Vivo);

        var cornelia = NuevoPersonaje(obra, "Cornelia Carlyle", "Secundario",
            "La guardiana estricta / hermana mayor",
            "Sacrificó su vida por criar a Archie y teme perder su lugar en la casa.",
            "Controlar el hogar de los Carlyle y 'proteger' a su hermano.",
            "Soltar el control y dejar que Archie viva su propia vida.",
            "Tensiona el matrimonio de Isabel con su rigidez y vigilancia.",
            EstadoVital.Vivo);

        // ----- Ubicaciones -----
        var eastLynne = NuevaUbicacion(obra, "East Lynne", "Edificio",
            "La mansión-marca de los Vane; objeto de deseo y eje del cambio de manos. Carlyle la compra y se vuelve el hogar del matrimonio.",
            "En venta al inicio; adquirida por Archie Carlyle.");

        var westLynne = NuevaUbicacion(obra, "West Lynne", "Pueblo",
            "Comunidad pequeña y chismosa donde vive Carlyle; el 'ojo público' que juzga cada movimiento de Isabel.",
            "Pueblo activo; epicentro del rumor social.");

        var casaVane = NuevaUbicacion(obra, "Casa Vane (ciudad)", "Edificio",
            "Escenario de apertura: el estudio donde William espera la venta. Símbolo del lujo que se desmorona.",
            "Semivacía, en proceso de desmantelamiento por embargos.");

        // ----- Beats -----
        NuevoBeat(obra, 1, "El mundo dorado se desmorona", Acto.Setup, FuncionNarrativa.Setup, BeatEstado.Escrito,
            "La fortuna Vane colapsa en público. Isabel pierde su burbuja de lujo sin entender aún la magnitud del desastre.");
        NuevoBeat(obra, 2, "Llega el hombre nuevo", Acto.Setup, FuncionNarrativa.Detonante, BeatEstado.Escrito,
            "Archie Carlyle compra East Lynne y conoce a Isabel. Chispa contenida que detonará todo el drama.");
        NuevoBeat(obra, 3, "Huérfana y sin nada", Acto.Acto1, FuncionNarrativa.Giro, BeatEstado.Pendiente,
            "Muere William. Isabel queda sin dinero, sin estatus y sin red de apoyo: por primera vez el mundo real la toca.");
        NuevoBeat(obra, 4, "Matrimonio por refugio", Acto.Acto1, FuncionNarrativa.Giro, BeatEstado.Pendiente,
            "Isabel se casa con Carlyle: seguridad y cariño real, pero sin la pasión que ella idealiza. Tensión con Cornelia.");
        NuevoBeat(obra, 5, "La tentación reaparece", Acto.Acto2, FuncionNarrativa.PuntoMedio, BeatEstado.Pendiente,
            "Francis Levison se cuela en su vida. Celos mal manejados y vacío emocional empujan a Isabel hacia el abismo.");
        NuevoBeat(obra, 6, "El error irreversible", Acto.Acto2, FuncionNarrativa.Crisis, BeatEstado.Pendiente,
            "Isabel abandona a marido e hijos por Levison. El impulso que no se puede deshacer.");
        NuevoBeat(obra, 7, "La caída y el descarte", Acto.Acto3, FuncionNarrativa.Giro, BeatEstado.Pendiente,
            "Levison la descarta cuando deja de serle útil. Isabel toca fondo: sin familia, sin nombre, sin nadie.");
        NuevoBeat(obra, 8, "Volver disfrazada", Acto.Acto3, FuncionNarrativa.Climax, BeatEstado.Pendiente,
            "Isabel regresa con otra identidad como cuidadora de sus propios hijos, viviendo bajo el techo de Carlyle —ya con Barbara.");
        NuevoBeat(obra, 9, "La verdad y el adiós", Acto.Acto3, FuncionNarrativa.Resolucion, BeatEstado.Pendiente,
            "La identidad de Isabel se revela en el peor momento posible. Desenlace trágico: el precio total de un único impulso.");

        // ----- Eventos (con relaciones N:N) -----
        NuevoEvento(obra, 1, "William hereda la fortuna por azar", Acto.Backstory, EventoTipo.Backstory,
            "~30 años antes del relato",
            "Un estudiante de Derecho sin un peso se convierte de golpe en magnate al morir tres herederos.",
            new[] { william }, Array.Empty<Ubicacion>());
        NuevoEvento(obra, 2, "Veinte años de disipación", Acto.Backstory, EventoTipo.Backstory,
            "Las dos décadas siguientes",
            "La fortuna se evapora en lujo y excesos; nacen las deudas que lo arruinarán.",
            new[] { william }, Array.Empty<Ubicacion>());
        NuevoEvento(obra, 3, "La ruina se hace pública", Acto.Setup, EventoTipo.Canonico,
            "Presente, justo antes del Cap. 1",
            "Embargos y prensa rosa; la familia debe vender East Lynne para sobrevivir.",
            new[] { william, isabel }, new[] { casaVane });
        NuevoEvento(obra, 4, "Carlyle compra East Lynne y conoce a Isabel", Acto.Setup, EventoTipo.Canonico,
            "Capítulo 1",
            "La propiedad cambia de manos; nace el vínculo Isabel–Carlyle.",
            new[] { isabel, carlyle, william }, new[] { casaVane, eastLynne });
        NuevoEvento(obra, 5, "Muerte de William; Isabel queda sin nada", Acto.Acto1, EventoTipo.Canonico,
            "Poco después de la venta",
            "Isabel pierde estatus, hogar y red de apoyo; queda a la intemperie social.",
            new[] { william, isabel }, Array.Empty<Ubicacion>());
        NuevoEvento(obra, 6, "Isabel se casa con Carlyle", Acto.Acto1, EventoTipo.Canonico,
            "Meses después",
            "Isabel gana refugio y estabilidad; aparece la tensión doméstica con Cornelia.",
            new[] { isabel, carlyle, cornelia }, new[] { eastLynne });
        NuevoEvento(obra, 7, "Levison seduce a Isabel; ella abandona a su familia", Acto.Acto2, EventoTipo.Canonico,
            "Clímax del Acto 2",
            "Ruptura irreversible y escándalo público: Isabel deja a marido e hijos.",
            new[] { isabel, levison, carlyle }, Array.Empty<Ubicacion>());
        NuevoEvento(obra, 8, "Isabel regresa disfrazada a cuidar a sus hijos", Acto.Acto3, EventoTipo.Canonico,
            "Tras tocar fondo",
            "Isabel vive junto a la familia que perdió sin poder revelarse; Barbara ya ocupa su lugar.",
            new[] { isabel, carlyle, barbara }, new[] { eastLynne });

        // West Lynne queda como ubicación de ambiente (el 'ojo público'); no se ata a un evento puntual.
        _ = westLynne;

        await db.Obras.AddAsync(obra, ct);

        db.RegistrosPaso.Add(new RegistroPaso
        {
            Id = Guid.NewGuid(),
            ObraId = obra.Id,
            Agente = "Seeder",
            Accion = "Carga del ejemplo canónico East Lynne",
            Cambios = $"{obra.Personajes.Count} personajes, {obra.Ubicaciones.Count} ubicaciones, " +
                      $"{obra.Beats.Count} beats, {obra.Eventos.Count} eventos.",
            Timestamp = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync(ct);
        return true;
    }

    private static Personaje NuevoPersonaje(Obra obra, string nombre, string rol, string arquetipo,
        string herida, string deseo, string necesidad, string estadoEnTrama, EstadoVital vital)
    {
        var p = new Personaje
        {
            Id = Guid.NewGuid(),
            ObraId = obra.Id,
            Nombre = nombre,
            Rol = rol,
            Arquetipo = arquetipo,
            HeridaCentral = herida,
            Deseo = deseo,
            Necesidad = necesidad,
            EstadoEnTrama = estadoEnTrama,
            EstadoVital = vital,
            Canon = CanonNivel.Canonico
        };
        obra.Personajes.Add(p);
        return p;
    }

    private static Ubicacion NuevaUbicacion(Obra obra, string nombre, string tipo, string rol, string estado)
    {
        var u = new Ubicacion
        {
            Id = Guid.NewGuid(),
            ObraId = obra.Id,
            Nombre = nombre,
            Tipo = tipo,
            RolEnTrama = rol,
            EstadoActual = estado,
            Canon = CanonNivel.Canonico
        };
        obra.Ubicaciones.Add(u);
        return u;
    }

    private static void NuevoBeat(Obra obra, int orden, string titulo, Acto acto,
        FuncionNarrativa funcion, BeatEstado estado, string descripcion)
    {
        obra.Beats.Add(new Beat
        {
            Id = Guid.NewGuid(),
            ObraId = obra.Id,
            Orden = orden,
            Titulo = titulo,
            Acto = acto,
            Funcion = funcion,
            Estado = estado,
            Descripcion = descripcion
        });
    }

    private static void NuevoEvento(Obra obra, int orden, string titulo, Acto acto, EventoTipo tipo,
        string momento, string consecuencia, Personaje[] personajes, Ubicacion[] ubicaciones)
    {
        var e = new Evento
        {
            Id = Guid.NewGuid(),
            ObraId = obra.Id,
            Orden = orden,
            Titulo = titulo,
            Acto = acto,
            Tipo = tipo,
            MomentoInWorld = momento,
            CambioConsecuencia = consecuencia
        };
        foreach (var p in personajes) e.Personajes.Add(p);
        foreach (var u in ubicaciones) e.Ubicaciones.Add(u);
        obra.Eventos.Add(e);
    }
}
