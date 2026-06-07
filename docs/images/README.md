# Screenshots needed

To complete the visual hook in the project README, capture these
screens after running `dotnet run --project src/CenitStoryTeller.Web`
on a clean DB (`--migrate` applied + East Lynne demo loaded).

The README and `docs/architecture.md` already reference these paths,
so dropping the files in here makes them appear without further edits.

| File name                  | Page          | What to capture                                                                        |
|----------------------------|---------------|----------------------------------------------------------------------------------------|
| `01-workspace.png`         | `/obras/{id}` | Obra workspace with tabs visible, East Lynne canon loaded (Capítulos tab is fine).     |
| `02-acid-failed.png`       | Capítulo view | A `CapituloVersion` whose acid test failed, showing Hallazgos + Parches + regen panel. |
| `03-configuracion.png`     | `/configuracion` | Provider dropdown open, an API key entered, "Probar conexión" button.               |
| `04-beat-reorder.png`      | Beats tab     | A few beats with ↑/↓ buttons + "Insertar aquí" link between two rows.                  |

A 15-second `demo.gif` (optional but high impact) of the full loop —
login → modernizar → workspace → ácido falla → regenerar — turns
visitors into testers. Tools that work well:
- [ScreenToGif](https://www.screentogif.com/) on Windows
- [LICEcap](https://www.cockos.com/licecap/) cross-platform
- [Peek](https://github.com/phw/peek) on Linux

Keep files under 1 MB each. If the PNGs are bigger, run them through
`pngquant` or `tinypng`.
