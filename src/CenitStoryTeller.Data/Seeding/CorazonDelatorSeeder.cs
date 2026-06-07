using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CenitStoryTeller.Core.Entities;

namespace CenitStoryTeller.Data.Seeding;

// Segundo demo: "El corazón delator" (Poe, 1843; dominio público).
// Cuento breve, dramático, en primera persona — opuesto en escala y tono al
// melodrama largo de East Lynne. Sirve para mostrar que el framework no está
// hardcoded a un género o longitud específica.
public static class CorazonDelatorSeeder
{
    public const string TituloObra = "El corazón delator";

    public static async Task<bool> SeedAsync(NovelaDbContext db, CancellationToken ct = default)
    {
        if (await db.Obras.AnyAsync(o => o.Titulo == TituloObra, ct))
            return false;

        var obra = new Obra
        {
            Id = Guid.NewGuid(),
            Titulo = TituloObra,
            Medio = "Cuento corto",
            Genero = "Horror psicológico / monólogo del culpable",
            Logline = "Un narrador convencido de su lucidez asesina a un anciano para acallar " +
                      "su ojo de buitre, y termina delatándose a sí mismo cuando el latido del " +
                      "muerto se vuelve insoportable.",
            Intake = IntakeTipo.DominioPublico,
            Estado = ObraEstado.Biblia,
            PlataformaObjetivo = "Wattpad",
            TemaCentral = "La culpa que el narrador no admite tener, manifestándose como sonido."
        };

        // ----- Personajes (3 — la obra es deliberadamente intima) -----
        var narrador = NuevoPersonaje(obra, "El narrador", "Protagonista",
            "Obsesivo lúcido / homicida en negación",
            "Cree que su exceso de sensibilidad prueba su cordura cuando en realidad la delata. " +
            "Identidad atada a sentirse en control de sus propios sentidos.",
            "Demostrarle a quien escucha — y a sí mismo — que actuó con razón.",
            "Reconocer la culpa que su mente fragmenta en ruido externo.",
            "Empieza relatando con calma; al final se desmorona y confiesa.",
            EstadoVital.Vivo);

        var anciano = NuevoPersonaje(obra, "El anciano", "Antagonista",
            "Víctima inocente / objeto de obsesión",
            "Ninguna en él mismo — su 'culpa' es tener un ojo deforme que el narrador no puede tolerar.",
            "Vivir tranquilo en su casa.",
            "Sobrevivir esta noche (no lo logra).",
            "Vivo durante los actos 1 y la primera mitad del 2. Muere en el clímax del acto 2.",
            EstadoVital.Muerto);

        var policias = NuevoPersonaje(obra, "Los policías", "Secundario",
            "Inspectores cordiales",
            "Ninguna; son catalizadores neutros.",
            "Investigar la denuncia del grito que oyó el vecino.",
            "Confirmar que todo está en orden y retirarse.",
            "Llegan tras el asesinato; se sientan tranquilos y desencadenan, sin saberlo, la confesión.",
            EstadoVital.Vivo);

        // ----- Ubicaciones -----
        var casa = NuevaUbicacion(obra, "Casa del anciano", "Casa modesta nocturna",
            "Único escenario de toda la historia. Su quietud amplifica cualquier ruido — base " +
            "auditiva de toda la tensión.",
            "Limpia, ordenada, en silencio. Una habitación con cama y un farol.");

        var habitacion = NuevaUbicacion(obra, "La alcoba", "Habitación del anciano",
            "Lugar del crimen. La cama, el suelo donde quedaron las tres tablas que el narrador levantó.",
            "Tras el asesinato, las tablas vuelven a su lugar; la apariencia de orden es perfecta.");

        // ----- Beats (estructura mínima de tragedia clásica adaptada al cuento) -----
        NuevoBeat(obra, 1, "Declaración de cordura", Acto.Setup,
            FuncionNarrativa.Setup, BeatEstado.Validado,
            "El narrador insiste en que está sano y propone probarlo contando cómo " +
            "planeó el crimen con una calma quirúrgica.");

        NuevoBeat(obra, 2, "El ojo del buitre como detonante", Acto.Setup,
            FuncionNarrativa.Detonante, BeatEstado.Validado,
            "Establece la obsesión específica: no odia al anciano, odia su ojo. La " +
            "fijación es desproporcionada y eso es lo que el narrador no logra ver.");

        NuevoBeat(obra, 3, "Siete noches de espiar", Acto.Acto1,
            FuncionNarrativa.Giro, BeatEstado.Validado,
            "Cada noche el narrador abre la puerta lentísimamente para observar al " +
            "anciano dormido. La paciencia es su prueba de cordura ante sí mismo.");

        NuevoBeat(obra, 4, "La octava noche, el anciano despierta", Acto.Acto2,
            FuncionNarrativa.PuntoMedio, BeatEstado.Validado,
            "El anciano oye el resorte de la linterna y se incorpora. Quedan en " +
            "silencio una hora. El narrador escucha el latido del corazón del anciano " +
            "y empieza a obsesionarse con su volumen.");

        NuevoBeat(obra, 5, "El asesinato", Acto.Acto2,
            FuncionNarrativa.Crisis, BeatEstado.Validado,
            "Convencido de que el latido va a despertar al vecindario, el narrador se " +
            "lanza sobre el anciano, lo derriba con la cama y lo asfixia. Confirma " +
            "la muerte: el latido se detiene.");

        NuevoBeat(obra, 6, "El cuerpo bajo las tablas", Acto.Acto2,
            FuncionNarrativa.Giro, BeatEstado.Validado,
            "Descuartiza el cuerpo en la bañera, esconde las partes bajo tres tablas " +
            "del suelo de la alcoba y deja todo limpio. Está orgulloso de su orden.");

        NuevoBeat(obra, 7, "Llegan los policías", Acto.Acto3,
            FuncionNarrativa.Detonante, BeatEstado.Validado,
            "Un vecino oyó un grito y los inspectores vienen a verificar. El narrador " +
            "los recibe con confianza, les cuenta que el anciano viajó al campo, y los " +
            "invita a sentarse — irónicamente — sobre las mismas tablas.");

        NuevoBeat(obra, 8, "El latido invade el cuarto", Acto.Acto3,
            FuncionNarrativa.Climax, BeatEstado.Validado,
            "Mientras los policías charlan tranquilos, el narrador empieza a oír un " +
            "latido bajo el suelo. Sube de volumen. Está seguro de que ellos también " +
            "lo oyen y se burlan en silencio.");

        NuevoBeat(obra, 9, "La confesión histérica", Acto.Acto3,
            FuncionNarrativa.Resolucion, BeatEstado.Validado,
            "Incapaz de soportar el sonido, el narrador grita la confesión, arranca las " +
            "tablas del suelo y señala el corazón del muerto. El cuento se cierra " +
            "exactamente donde su negación lo expuso.");

        // ----- Eventos N:N (qué pasa entre qué personajes y dónde) -----
        NuevoEvento(obra, 1, "Decisión secreta", Acto.Setup, EventoTipo.Canonico,
            "Una semana antes del crimen, durante el día",
            "Compromiso interno del narrador con su plan; nace la 'razón' (el ojo).",
            new[] { narrador }, new[] { casa });

        NuevoEvento(obra, 2, "Vigilias nocturnas", Acto.Acto1, EventoTipo.Canonico,
            "Siete noches consecutivas, exactamente a medianoche",
            "El narrador prueba para sí su autocontrol; la obsesión se reafirma cada noche.",
            new[] { narrador, anciano }, new[] { habitacion });

        NuevoEvento(obra, 3, "El despertar del anciano", Acto.Acto2, EventoTipo.Canonico,
            "Octava noche, una hora antes del amanecer",
            "El silencio se rompe; el latido empieza a sonar para el narrador.",
            new[] { narrador, anciano }, new[] { habitacion });

        NuevoEvento(obra, 4, "Asesinato y ocultamiento", Acto.Acto2, EventoTipo.Canonico,
            "Madrugada de la octava noche",
            "El anciano muere; el narrador queda 'seguro' de haber resuelto el problema.",
            new[] { narrador, anciano }, new[] { habitacion });

        NuevoEvento(obra, 5, "Llegada de los policías", Acto.Acto3, EventoTipo.Canonico,
            "Esa misma mañana al alba",
            "Catalizador externo: la calma fingida del narrador será puesta a prueba.",
            new[] { narrador, policias }, new[] { casa });

        NuevoEvento(obra, 6, "Confesión", Acto.Acto3, EventoTipo.Canonico,
            "Pocos minutos después, al alba",
            "El narrador se delata; la 'cordura' que defendía colapsa en público.",
            new[] { narrador, policias }, new[] { habitacion });

        // ----- Capítulos (sin versiones; el seeder solo arma la estructura) -----
        // Beats 1+2 quedan en el capítulo 1 (intro). El resto se mapea 1:1.
        NuevoCapitulo(obra, 1, "Soy un hombre cuerdo", obra.Beats.First(b => b.Orden == 1));
        NuevoCapitulo(obra, 2, "Siete noches en la puerta",   obra.Beats.First(b => b.Orden == 3));
        NuevoCapitulo(obra, 3, "El despertar",                obra.Beats.First(b => b.Orden == 4));
        NuevoCapitulo(obra, 4, "El acto",                     obra.Beats.First(b => b.Orden == 5));
        NuevoCapitulo(obra, 5, "Bajo las tablas",             obra.Beats.First(b => b.Orden == 6));
        NuevoCapitulo(obra, 6, "Una visita amable",           obra.Beats.First(b => b.Orden == 7));
        NuevoCapitulo(obra, 7, "¡Aquí, aquí!",                obra.Beats.First(b => b.Orden == 9));

        await db.Obras.AddAsync(obra, ct);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static Personaje NuevoPersonaje(Obra obra, string nombre, string rol, string arquetipo,
        string herida, string deseo, string necesidad, string estado, EstadoVital vital)
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
            EstadoEnTrama = estado,
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

    private static void NuevoCapitulo(Obra obra, int orden, string titulo, Beat beatObjetivo)
    {
        obra.Capitulos.Add(new Capitulo
        {
            Id = Guid.NewGuid(),
            ObraId = obra.Id,
            Orden = orden,
            Titulo = titulo,
            Estado = CapituloEstado.Esquema,
            BeatObjetivoId = beatObjetivo.Id
        });
    }
}
