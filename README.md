# SimpleExampleInvoice

Sistema web para crear facturas, agregar detalles, previsualizar e imprimir en PDF.

## Caracteristicas

- Login basico por cookies.
- Creacion de factura bajo demanda (no se crea automaticamente al entrar).
- Edicion de cliente y empresa.
- Detalles de factura (descripcion, cantidad, precio).
- Previsualizacion e impresion en PDF.
- Calculo automatico de ITBIS (18%) en el PDF.
- Toast de confirmacion al crear factura.

## Stack tecnico

- ASP.NET Core MVC (`net10.0`)
- Entity Framework Core + SQL Server
- QuestPDF

## Requisitos

- .NET SDK 10.x
- SQL Server accesible
- Credenciales de base de datos

## Configuracion local

1. Clonar repositorio.
2. Configurar `ConnectionStrings:DefaultConnection` en `appsettings.json` o `appsettings.Development.json`.
3. Restaurar dependencias:

```bash
dotnet restore
```

4. Aplicar migraciones:

```bash
dotnet ef database update
```

5. Ejecutar:

```bash
dotnet run
```

## Publicacion para produccion

1. Configurar connection string de produccion.
2. Publicar en Release:

```bash
dotnet publish -c Release -o ./publish
```

3. Subir contenido de `publish/` al servidor.
4. Ejecutar migraciones sobre la base de datos de produccion.

## Despliegue en SmarterASP

1. Verifica que el hosting soporte la version de .NET usada por el proyecto.
2. Configura en el panel el connection string de SQL Server.
3. Publica en modo Release y sube los archivos a la carpeta del sitio.
4. Confirma que el sitio tenga SSL habilitado para HTTPS.
5. Si el proveedor lo permite, ejecuta migraciones antes de abrir al publico.

## Script de automatizacion

Se incluye:

- `scripts/install_and_deploy.sh`

Este script automatiza:

- restore
- build
- migraciones (opcional)
- publish en carpeta `publish/`

Uso:

```bash
chmod +x scripts/install_and_deploy.sh
./scripts/install_and_deploy.sh
```

Opciones utiles:

```bash
./scripts/install_and_deploy.sh --skip-migrate
./scripts/install_and_deploy.sh --configuration Debug
./scripts/install_and_deploy.sh --output ./publish-prod
```

## Soporte

Desarrollado por Pedro Peguero.

Para cualquier informacion, solo WhatsApp: **829-966-1111**.
