# Hotel Web — Laboratorio de semana 10

Aplicación ASP.NET Core MVC con EF Core y SQL Server, desarrollada siguiendo el PPT y los requisitos específicos del laboratorio. El laboratorio exige repositorios y baja lógica, aunque los ejemplos generales del PPT muestran consultas directas en el controlador y borrado físico.

## Abrir y ejecutar

Requisitos: SDK .NET 10, SQL Server o SQL Server LocalDB y acceso a NuGet para restaurar paquetes. Abrir `../Hotel.slnx` en Visual Studio o esta carpeta en VS Code.

La base ya está preparada en la instancia `(localdb)\MSSQLLocalDB` de este equipo. Para otro equipo, ejecutar en SQL Server primero `database/01-HotelDB.sql` y después `database/02-Observaciones.sql`. **El script original del docente elimina y recrea HotelDB si ya existe; ejecutarlo solo en una base de prácticas que pueda reemplazarse.**

Ajustar `ConnectionStrings:Hotel` en `appsettings.json` si se usa otra instancia. Desde esta carpeta:

```powershell
dotnet restore
dotnet run --no-launch-profile --urls http://localhost:5080
```

Abrir http://localhost:5080. Las rutas son `/Habitaciones`, `/Reservas/Disponibles` y `/Reservas`.

## Scaffold utilizado

Se ejecutó scaffold real sobre SQL Server, sin editar las entidades manualmente. La opción `--no-onconfiguring` impide generar el método que el laboratorio pide quitar. El contexto recibe la conexión exclusivamente mediante `AddDbContext`.

Comando inicial, desde la carpeta superior a Hotel.Web:

```powershell
.\.tools\dotnet-ef dbcontext scaffold "Server=(localdb)\MSSQLLocalDB;Database=HotelDB;Trusted_Connection=True;TrustServerCertificate=True" Microsoft.EntityFrameworkCore.SqlServer --project Hotel.Web --output-dir Models --context HotelContext --no-onconfiguring
```

Después se ejecutó `ALTER TABLE Reserva ADD Observaciones NVARCHAR(200) NULL;` y se repitió el comando añadiendo `--force --no-build`. Se usó `--no-build` porque los formularios ya referenciaban la propiedad nueva y la compilación previa correspondía al modelo inicial. Tras regenerar, se compiló la solución completa.

Para reproducir el scaffold en otra máquina, desde Hotel.Web:

```powershell
dotnet tool install --global dotnet-ef --version 10.0.11
dotnet ef dbcontext scaffold "Name=ConnectionStrings:Hotel" Microsoft.EntityFrameworkCore.SqlServer --output-dir Models --context HotelContext --no-onconfiguring --force
```

En la consola de paquetes de Visual Studio, seleccionar Hotel.Web como proyecto predeterminado:

```powershell
Scaffold-DbContext "Name=ConnectionStrings:Hotel" Microsoft.EntityFrameworkCore.SqlServer -OutputDir Models -Context HotelContext -NoOnConfiguring -Force
```

## Requisitos implementados

- Habitaciones: listado activo con Include, filtro por tipo, orden por número, detalle, alta, edición y baja lógica. Se valida número único incluyendo habitaciones inactivas, longitud, piso y tipo existente.
- Reservas: consulta de disponibilidad con una sola consulta LINQ y `!Any`, listas de huéspedes y habitaciones activos, creación Pendiente y cancelación exclusivamente de Pendiente o Confirmada.
- Se rechazan fechas inválidas, ingreso pasado, entidades inactivas y reservas superpuestas. Las reservas canceladas no bloquean. La salida y el ingreso contiguos no se cruzan.
- Noches y total: diferencia entre fechas multiplicada por PrecioNoche. El esquema no guarda tarifa histórica; el listado usa la tarifa actual del tipo.
- Repositorios con consultas de lectura AsNoTracking, métodos async y SaveChangesAsync. Los controladores reciben únicamente repositorios. Sin migraciones, procedimientos almacenados ni SQL manual en la aplicación.
- Vistas tipadas directamente a las entidades. ModelState.Remove para navegaciones, errores mediante ModelState.AddModelError, formularios GET/POST, antifalsificación, redirección después de guardar y TempData.
- Observaciones generada desde la base y visible en Create e Index, con límite de 200 caracteres.
- Transacción Serializable al reservar para revalidar la disponibilidad antes de guardar.

## Evidencias y comprobaciones

`evidencias/` incluye capturas reales y el resultado de 26 comprobaciones funcionales contra SQL Server. `verificacion/probar.py` permite revisar los casos contra el sitio iniciado. Crea una habitación de prueba que queda inactiva y una reserva que termina cancelada, conservando el historial. Ejecutarlo en una base de prácticas recién creada para evitar colisiones con la prueba anterior.

`Entrega-Lab10.docx` reúne el comando de scaffold, explicación, capturas, observaciones y conclusiones.

## Repositorio

Repositorio del proyecto: https://github.com/sesema98/reservas-hotel-lab10-serva. El código está en Hotel.Web y la solución Hotel.slnx está en la raíz.

## Observaciones y conclusiones

Database First mantiene el esquema SQL como fuente del modelo. Observaciones se incorporó mediante ALTER TABLE y regeneración, sin modificar manualmente las entidades. La separación en repositorios concentra el acceso a datos y permite que los controladores se ocupen del formulario y de la navegación. La regla de intervalos abiertos en la salida permite reservas contiguas, y la baja lógica conserva la relación de las reservas existentes.
