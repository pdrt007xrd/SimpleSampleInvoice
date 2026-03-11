#!/usr/bin/env python3
"""
=============================================================
  deploy.py — Script de despliegue para SimpleSampleInvoice
  ASP.NET Core MVC (.NET 10) + Nginx + SQL Server
  Autor: Pedro Peguero  |  Soporte: 829-966-1111
=============================================================

Uso:
    sudo python3 deploy.py [opciones]

Opciones:
    --repo-url      URL del repositorio (default: repo original)
    --app-dir       Directorio destino del proyecto (default: /var/www/SimpleSampleInvoice)
    --publish-dir   Directorio de publicacion       (default: /var/www/SimpleSampleInvoice/publish)
    --port          Puerto interno de Kestrel        (default: 5000)
    --domain        Dominio o IP para nginx          (default: localhost)
    --skip-migrate  Omitir migraciones de EF Core
    --skip-nginx    Omitir configuracion de nginx
    --skip-service  Omitir creacion del servicio systemd
    --env           Entorno: Development | Production (default: Production)

Ejemplo completo:
    sudo python3 deploy.py \\
        --domain facturacion.miempresa.com \\
        --port 5000 \\
        --app-dir /var/www/invoice

Ejemplo rapido (localhost):
    sudo python3 deploy.py --skip-migrate
"""

import argparse
import os
import subprocess
import sys
import textwrap
from pathlib import Path
from datetime import datetime

# ─── Colores ANSI ────────────────────────────────────────────────────────────
RED    = "\033[91m"
GREEN  = "\033[92m"
YELLOW = "\033[93m"
BLUE   = "\033[94m"
CYAN   = "\033[96m"
BOLD   = "\033[1m"
RESET  = "\033[0m"

def log(msg, color=RESET):        print(f"{color}{msg}{RESET}")
def ok(msg):                       log(f"  ✔  {msg}", GREEN)
def info(msg):                     log(f"  ➜  {msg}", CYAN)
def warn(msg):                     log(f"  ⚠  {msg}", YELLOW)
def err(msg):                      log(f"  ✖  {msg}", RED)
def header(title):
    sep = "─" * 60
    print(f"\n{BOLD}{BLUE}{sep}\n  {title}\n{sep}{RESET}")

# ─── Ejecución de comandos ────────────────────────────────────────────────────
def run(cmd, cwd=None, env=None, check=True):
    """Ejecuta un comando y retorna el proceso."""
    info(f"$ {cmd}")
    result = subprocess.run(
        cmd, shell=True, cwd=cwd, env=env,
        stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True
    )
    if result.stdout.strip():
        print(f"    {result.stdout.strip()}")
    if result.returncode != 0:
        if result.stderr.strip():
            err(result.stderr.strip())
        if check:
            err(f"Comando fallido (código {result.returncode}): {cmd}")
            sys.exit(1)
    return result

def run_ok(cmd, cwd=None, env=None):
    """Ejecuta y retorna True/False sin abortar."""
    r = run(cmd, cwd=cwd, env=env, check=False)
    return r.returncode == 0

# ─── Verificaciones de requisitos ────────────────────────────────────────────
def check_root():
    if os.geteuid() != 0:
        err("Este script debe ejecutarse como root o con sudo.")
        sys.exit(1)

def check_command(cmd, install_hint=""):
    result = subprocess.run(f"which {cmd}", shell=True, capture_output=True, text=True)
    if result.returncode != 0:
        err(f"'{cmd}' no encontrado.")
        if install_hint:
            warn(f"Instálalo con: {install_hint}")
        sys.exit(1)
    ok(f"{cmd} → {result.stdout.strip()}")

def check_dotnet_version(required="10"):
    result = subprocess.run("dotnet --version", shell=True, capture_output=True, text=True)
    if result.returncode != 0:
        err(".NET SDK no encontrado. Instálalo desde https://dot.net")
        sys.exit(1)
    version = result.stdout.strip()
    if not version.startswith(required):
        warn(f".NET SDK versión {version} detectada. El proyecto requiere .NET {required}.x")
        warn("Continuando de todas formas...")
    else:
        ok(f"dotnet {version}")

# ─── Paso 1: Clonar o actualizar repo ────────────────────────────────────────
def step_clone_or_pull(repo_url: str, app_dir: Path):
    header("PASO 1 — Repositorio")
    git_dir = app_dir / ".git"
    if git_dir.exists():
        info(f"Repositorio ya existe en {app_dir}. Actualizando...")
        run("git pull origin main", cwd=str(app_dir))
        ok("Repositorio actualizado.")
    else:
        info(f"Clonando {repo_url} → {app_dir}")
        app_dir.parent.mkdir(parents=True, exist_ok=True)
        run(f"git clone {repo_url} {app_dir}")
        ok("Repositorio clonado.")

# ─── Paso 2: Restaurar dependencias ──────────────────────────────────────────
def step_restore(app_dir: Path):
    header("PASO 2 — Restaurar dependencias")
    run("dotnet restore", cwd=str(app_dir))
    ok("Dependencias restauradas.")

# ─── Paso 3: Migraciones ─────────────────────────────────────────────────────
def step_migrate(app_dir: Path, dotnet_env: str):
    header("PASO 3 — Migraciones Entity Framework Core")
    env = {**os.environ, "ASPNETCORE_ENVIRONMENT": dotnet_env}
    # Verificar que dotnet-ef esté instalado
    ef_check = run_ok("dotnet ef --version", cwd=str(app_dir), env=env)
    if not ef_check:
        warn("dotnet-ef no encontrado. Instalando...")
        run("dotnet tool install --global dotnet-ef", env=env)
        # Agregar ~/.dotnet/tools al PATH temporalmente
        tools_path = Path.home() / ".dotnet" / "tools"
        env["PATH"] = f"{tools_path}:{env.get('PATH', '')}"
    run("dotnet ef database update", cwd=str(app_dir), env=env)
    ok("Migraciones aplicadas.")

# ─── Paso 4: Publicar ────────────────────────────────────────────────────────
def step_publish(app_dir: Path, publish_dir: Path, config: str):
    header("PASO 4 — Publicar aplicación")
    publish_dir.mkdir(parents=True, exist_ok=True)
    run(
        f"dotnet publish -c {config} -o {publish_dir} --no-restore",
        cwd=str(app_dir)
    )
    ok(f"Publicado en {publish_dir}")

# ─── Paso 5: Servicio systemd ─────────────────────────────────────────────────
SERVICE_NAME = "simpleinvoice"

def step_systemd(publish_dir: Path, port: int, dotnet_env: str):
    header("PASO 5 — Servicio systemd")

    # Buscar el ejecutable publicado
    executables = list(publish_dir.glob("SimpleExampleInvoice"))
    if not executables:
        executables = list(publish_dir.glob("*.dll"))
    
    # Usamos dotnet <dll> para correr la app
    dll_files = list(publish_dir.glob("SimpleExampleInvoice.dll"))
    if dll_files:
        exec_cmd = f"/usr/bin/dotnet {dll_files[0]}"
    else:
        # Fallback: buscar cualquier .dll principal
        dlls = [d for d in publish_dir.glob("*.dll") if "Migration" not in d.name]
        if dlls:
            exec_cmd = f"/usr/bin/dotnet {dlls[0]}"
        else:
            err("No se encontró el .dll de la aplicación en el directorio de publicación.")
            sys.exit(1)

    service_content = textwrap.dedent(f"""\
        [Unit]
        Description=SimpleSampleInvoice — App de Facturacion
        After=network.target

        [Service]
        WorkingDirectory={publish_dir}
        ExecStart={exec_cmd}
        Restart=always
        RestartSec=10
        SyslogIdentifier={SERVICE_NAME}
        User=www-data
        Environment=ASPNETCORE_ENVIRONMENT={dotnet_env}
        Environment=ASPNETCORE_URLS=http://127.0.0.1:{port}
        Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

        [Install]
        WantedBy=multi-user.target
    """)

    service_path = Path(f"/etc/systemd/system/{SERVICE_NAME}.service")
    service_path.write_text(service_content)
    ok(f"Servicio escrito en {service_path}")

    # Ajustar permisos
    run(f"chown -R www-data:www-data {publish_dir}", check=False)

    run("systemctl daemon-reload")
    run(f"systemctl enable {SERVICE_NAME}")
    run(f"systemctl restart {SERVICE_NAME}")

    import time; time.sleep(2)
    status = run(f"systemctl is-active {SERVICE_NAME}", check=False)
    if status.stdout.strip() == "active":
        ok(f"Servicio '{SERVICE_NAME}' activo y corriendo en el puerto {port}.")
    else:
        warn(f"El servicio no está activo todavía. Revisa con: journalctl -u {SERVICE_NAME} -n 50")

# ─── Paso 6: Nginx ────────────────────────────────────────────────────────────
def step_nginx(domain: str, port: int):
    header("PASO 6 — Configuración Nginx")

    nginx_conf = textwrap.dedent(f"""\
        # SimpleSampleInvoice — Nginx reverse proxy
        # Generado por deploy.py el {datetime.now().strftime("%Y-%m-%d %H:%M")}

        server {{
            listen 80;
            server_name {domain};

            # Redirigir a HTTPS si tienes certificado SSL
            # return 301 https://$host$request_uri;

            location / {{
                proxy_pass         http://127.0.0.1:{port};
                proxy_http_version 1.1;
                proxy_set_header   Upgrade $http_upgrade;
                proxy_set_header   Connection keep-alive;
                proxy_set_header   Host $host;
                proxy_set_header   X-Real-IP $remote_addr;
                proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
                proxy_set_header   X-Forwarded-Proto $scheme;
                proxy_cache_bypass $http_upgrade;
                proxy_read_timeout 90;
            }}

            # Archivos estáticos servidos directamente por nginx (opcional)
            # location /wwwroot/ {{
            #     alias /var/www/SimpleSampleInvoice/publish/wwwroot/;
            #     expires 30d;
            # }}

            client_max_body_size 20M;
        }}

        # Bloque HTTPS — descomenta y ajusta si tienes SSL/Let's Encrypt
        # server {{
        #     listen 443 ssl;
        #     server_name {domain};
        #
        #     ssl_certificate     /etc/letsencrypt/live/{domain}/fullchain.pem;
        #     ssl_certificate_key /etc/letsencrypt/live/{domain}/privkey.pem;
        #     ssl_protocols       TLSv1.2 TLSv1.3;
        #     ssl_ciphers         HIGH:!aNULL:!MD5;
        #
        #     location / {{
        #         proxy_pass         http://127.0.0.1:{port};
        #         proxy_http_version 1.1;
        #         proxy_set_header   Upgrade $http_upgrade;
        #         proxy_set_header   Connection keep-alive;
        #         proxy_set_header   Host $host;
        #         proxy_set_header   X-Real-IP $remote_addr;
        #         proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
        #         proxy_set_header   X-Forwarded-Proto $scheme;
        #         proxy_cache_bypass $http_upgrade;
        #     }}
        # }}
    """)

    conf_path = Path(f"/etc/nginx/sites-available/{SERVICE_NAME}")
    conf_path.write_text(nginx_conf)
    ok(f"Configuración nginx escrita en {conf_path}")

    # Crear symlink en sites-enabled
    symlink = Path(f"/etc/nginx/sites-enabled/{SERVICE_NAME}")
    if symlink.exists() or symlink.is_symlink():
        symlink.unlink()
    symlink.symlink_to(conf_path)
    ok(f"Symlink creado en {symlink}")

    # Validar configuración nginx
    test = run("nginx -t", check=False)
    if test.returncode != 0:
        err("La configuración de nginx tiene errores. Revisando...")
        sys.exit(1)

    run("systemctl reload nginx")
    ok(f"Nginx recargado. App disponible en http://{domain}")

# ─── Resumen final ────────────────────────────────────────────────────────────
def print_summary(args, publish_dir: Path):
    header("DESPLIEGUE COMPLETADO")
    print(f"""
  {BOLD}App:{RESET}            SimpleSampleInvoice
  {BOLD}URL:{RESET}            http://{args.domain}
  {BOLD}Puerto Kestrel:{RESET} {args.port}
  {BOLD}Publicado en:{RESET}   {publish_dir}
  {BOLD}Entorno:{RESET}        {args.env}
  {BOLD}Servicio:{RESET}       {SERVICE_NAME}

  {YELLOW}Comandos útiles:{RESET}
    Ver logs:       journalctl -u {SERVICE_NAME} -f
    Reiniciar app:  systemctl restart {SERVICE_NAME}
    Estado app:     systemctl status {SERVICE_NAME}
    Reload nginx:   systemctl reload nginx
    Logs nginx:     tail -f /var/log/nginx/error.log

  {CYAN}Para habilitar HTTPS con Let's Encrypt:{RESET}
    sudo apt install certbot python3-certbot-nginx
    sudo certbot --nginx -d {args.domain}
    """)

# ─── CLI ─────────────────────────────────────────────────────────────────────
def parse_args():
    p = argparse.ArgumentParser(
        description="Script de despliegue para SimpleSampleInvoice",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=__doc__
    )
    p.add_argument("--repo-url",     default="https://github.com/pdrt007xrd/SimpleSampleInvoice.git")
    p.add_argument("--app-dir",      default="/var/www/SimpleSampleInvoice")
    p.add_argument("--publish-dir",  default=None,   help="Default: <app-dir>/publish")
    p.add_argument("--port",         default=5000,   type=int)
    p.add_argument("--domain",       default="localhost")
    p.add_argument("--env",          default="Production", choices=["Development", "Production"])
    p.add_argument("--configuration",default="Release",    choices=["Release", "Debug"])
    p.add_argument("--skip-migrate", action="store_true")
    p.add_argument("--skip-nginx",   action="store_true")
    p.add_argument("--skip-service", action="store_true")
    return p.parse_args()

# ─── Main ─────────────────────────────────────────────────────────────────────
def main():
    args = parse_args()
    app_dir     = Path(args.app_dir)
    publish_dir = Path(args.publish_dir) if args.publish_dir else app_dir / "publish"

    print(f"\n{BOLD}{CYAN}{'='*60}")
    print(f"  SimpleSampleInvoice — Deploy Script")
    print(f"  {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    print(f"{'='*60}{RESET}")

    # ── Verificaciones previas
    header("VERIFICACIONES PREVIAS")
    check_root()
    check_dotnet_version(required="10")
    check_command("git",   "apt install git")
    check_command("nginx", "apt install nginx")

    # ── Pasos del despliegue
    step_clone_or_pull(args.repo_url, app_dir)
    step_restore(app_dir)

    if not args.skip_migrate:
        step_migrate(app_dir, args.env)
    else:
        warn("Migraciones omitidas (--skip-migrate).")

    step_publish(app_dir, publish_dir, args.configuration)

    if not args.skip_service:
        step_systemd(publish_dir, args.port, args.env)
    else:
        warn("Servicio systemd omitido (--skip-service).")

    if not args.skip_nginx:
        step_nginx(args.domain, args.port)
    else:
        warn("Nginx omitido (--skip-nginx).")

    print_summary(args, publish_dir)

if __name__ == "__main__":
    main()
