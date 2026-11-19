using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanCom.Migrations.Migrations
{
    /// <summary>
    /// Carga la lista de tiendas 7clean y la bodega de insumos. Es idempotente: renombra las
    /// cuatro tiendas capturadas como "7c …" (conservan su id y su ubicación) e inserta las que
    /// falten por nombre. Solo trae ubicación donde OpenStreetMap encontró el número exacto;
    /// las demás se ubican con el mapa en Operación › Tiendas y bodegas.
    /// </summary>
    public partial class SeedTiendasYBodegas : Migration
    {
        private static readonly (string Name, string Type, string Address, double? Lat, double? Lng)[] Lugares =
        [
            ("Bodega EXO", "Bodega", "Calle Postes 22, Pólvora, Álvaro Obregón, 01150 CDMX", null, null),
            ("Florida", "Tienda", "Av. Insurgentes Sur 1706, Florida, Álvaro Obregón, 01030 CDMX", null, null),
            ("Del Valle", "Tienda", "Xola 407, Col. del Valle Nte, Benito Juárez, 03103 CDMX", null, null),
            ("Patio Universidad", "Tienda", "Av. Popocatépetl 546, Xoco, Benito Juárez, 03330 CDMX", 19.3655506, -99.1667950),
            ("Universidad", "Tienda", "Av. Universidad 740, Sta Cruz Atoyac, Benito Juárez, 03310 CDMX", 19.3737014, -99.1624349),
            ("Lomas", "Tienda", "Félix Berenguer 106, Lomas de Chapultepec, Miguel Hidalgo, 11000 CDMX", null, null),
            ("Polanco Ejército Nacional", "Tienda", "Av. Ejército Nacional 678, Polanco III Secc, Miguel Hidalgo, 11540 CDMX", null, null),
            ("Polanco Schiller", "Tienda", "Schiller 303, Polanco V Secc, Miguel Hidalgo, 11560 CDMX", null, null),
            ("Santa Fe", "Tienda", "Antonio Dovalí Jaime 70, Zedec Sta Fé, Álvaro Obregón, 01219 CDMX", 19.3683374, -99.2582739),
            ("San Ángel", "Tienda", "Av. Revolución 1349, Tlacopac, Álvaro Obregón, 01040 CDMX", null, null),
            ("City Pedregal", "Tienda", "Av. San Jerónimo 190, Tizapán San Ángel, Álvaro Obregón, 01090 CDMX", null, null),
            ("Desierto", "Tienda", "Desierto de los Leones 5260, Tetelpan, Álvaro Obregón, 01700 CDMX", 19.3394111, -99.2321649),
            ("Av. Toluca", "Tienda", "Av. Toluca 414, Olivar de los Padres, Álvaro Obregón, 01780 CDMX", null, null),
            ("Tlacoquemecatl", "Tienda", "C. Tlacoquemecatl 123, Tlacoquemecatl del Valle, Benito Juárez, 03200 CDMX", null, null),
            ("Dakota", "Tienda", "Dakota 110, Nápoles, Benito Juárez, 03810 CDMX", null, null),
            ("GranSur", "Tienda", "Blvd. Gran Sur 5550, El Caracol, Coyoacán, 04700 CDMX", null, null),
            ("Tlatelolco", "Tienda", "Calz. de la Ronda 88, Ex Hipódromo de Peralvillo, Cuauhtémoc, 06250 CDMX", 19.4558367, -99.1372680),
            ("Tecamachalco", "Tienda", "Tecamachalco 118, Lomas de Tecamachalco, 53950 Naucalpan, Edo. Méx.", null, null),
            ("Satélite", "Tienda", "Cto. Centro Comercial 16, Cd. Satélite, 53100 Naucalpan, Edo. Méx.", null, null),
            ("Sayavedra", "Tienda", "Av. Jorge Jiménez Cantú 1, Condado de Sayavedra, 52938 Atizapán, Edo. Méx.", null, null),
            ("San Fernando (7shoes / 7wash)", "Tienda", "Av. San Fernando 236, Tlalpan Centro I, Tlalpan, 14000 CDMX", null, null),
            ("Calzada del Hueso (7shoes / 7wash)", "Tienda", "Calz. del Hueso 281, Floresta Coyoacán, Tlalpan, 14310 CDMX", null, null),
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Las cuatro tiendas que ya existían con el prefijo "7c" toman el nombre de la lista.
            foreach (var (oldName, newName) in new[]
            {
                ("7c Florida", "Florida"),
                ("7c Del Valle", "Del Valle"),
                ("7c Polanco", "Polanco Schiller"),
                ("7c Lomas", "Lomas"),
            })
            {
                migrationBuilder.Sql($"""
                    UPDATE lugares SET name = N'{newName}', modified_at = SYSDATETIMEOFFSET()
                    WHERE name = N'{oldName}'
                      AND NOT EXISTS (SELECT 1 FROM lugares WHERE name = N'{newName}');
                    """);
            }

            foreach (var (name, type, address, lat, lng) in Lugares)
            {
                var location = lat is null
                    ? "NULL"
                    : FormattableString.Invariant($"geography::Point({lat}, {lng}, 4326)");
                migrationBuilder.Sql($"""
                    IF NOT EXISTS (SELECT 1 FROM lugares WHERE name = N'{name}')
                        INSERT INTO lugares (name, type, address, location, active, created_at)
                        VALUES (N'{name}', N'{type}', N'{address}', {location}, 1, SYSDATETIMEOFFSET());
                    """);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Nada se borra de la base: los lugares se quedan y se dan de baja desde la pantalla.
        }
    }
}
