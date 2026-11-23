# Christmas Pet (Compañero Navideño para Windows)

Un pequeño programa para Windows que muestra una animación de bienvenida navideña cada vez que inicias sesión y deja una mascota virtual paseando por encima de la barra de tareas, al estilo de los clásicos *Shimeji* o *Desktop Pets*.

Está programado en C# puro sobre `.NET Framework 4` y la API nativa de Windows (`Win32 Layered Windows`), por lo que no necesita instalar nada externo, pesa unos pocos kilobytes y consume alrededor de 15-30 MB de RAM.

---

## Qué hace el programa

### 1. Saludo inicial al encender la PC
Al iniciar sesión en Windows, aparece durante unos 8 segundos una superposición transparente a pantalla completa con:
- Una guirnalda de luces navideñas animadas en la parte superior del monitor.
- Copos de nieve cayendo a 60 FPS (mezclando copos de cristal que giran y partículas suaves al fondo).
- Un mensaje central de **¡Feliz Navidad!** junto con una cuenta regresiva automática que calcula cuántos días faltan para el 25 de diciembre según la fecha del sistema.

Toda esta pantalla inicial es *click-through*: aunque esté visible, los clics la atraviesan como si no estuviera ahí, así que no interrumpe si quieres abrir algo apenas arranca el escritorio.

### 2. Compañero virtual en la barra de tareas
Cuando termina el saludo inicial (o desde el primer segundo), queda activa tu mascota en el escritorio. Puedes elegir entre 5 personajes:
- **Duende Navideño**
- **Reno (Rudolph)**
- **Pingüino Invernal**
- **Gatito con Gorro**
- **Perrito Navideño**

Cada personaje tiene sus propios comportamientos:
- Camina por la barra de tareas, se detiene a descansar, se sienta, parpadea, salta y se queda dormido (cada uno tiene su propia postura para dormir).
- Si le das un **clic izquierdo**, hace un truco propio (el duende lanza un regalo, al reno se le iluminan los cuernos y saca una zanahoria, el pingüino se desliza de panza en el hielo, el gato saluda levantando la patita y el perro bate la cola con un hueso navideño) y dice una de sus frases.
- Si mantienes presionado el **clic izquierdo y mueves el ratón**, puedes arrastrarlo por cualquier parte de la pantalla. Al soltarlo, cae con gravedad hasta volver a pisar la barra de tareas.
- **Traspaso de pantalla y soporte para varios monitores:** cuando camina hasta el borde izquierdo de la pantalla, empieza a asomarse poco a poco por el borde derecho al mismo tiempo. Si tienes dos o más monitores conectados, camina libremente pasando de un monitor a otro y también puedes arrastrarlo al monitor que quieras.

---

## Estructura del proyecto

Para mantener la carpeta principal limpia, en la raíz solo está el ejecutable y todo lo demás vive dentro de `Archivos`:

```text
.
├── programadenavidadconunnombreinnecesariamentelargo.exe
└── Archivos/
    ├── CompaneroNavidad.cs          # Código fuente completo en un solo archivo
    ├── config.ini                   # Archivo donde se guardan tus ajustes y nombres
    ├── Configurar_Companero.bat     # Acceso rápido a la ventana de configuración
    ├── Desactivar_o_Desinstalar.bat # Cierra el programa y lo quita del arranque de Windows
    └── assets/
        ├── icon.png
        └── icon.ico
```

---

## Cómo usarlo y configurarlo

1. **Iniciar:** Abre `programadenavidadconunnombreinnecesariamentelargo.exe`. La primera vez que lo abras quedará configurado para iniciar automáticamente con Windows.
2. **Abrir la configuración:** Puedes abrir la ventana de ajustes de tres formas:
   - Dándole **clic derecho** directamente a la mascota y eligiendo `Configuración...`.
   - Dándole doble clic (o clic derecho) al icono navideño que aparece en la **bandeja del sistema** (junto al reloj de Windows).
   - Ejecutando `Archivos/Configurar_Companero.bat`.

### Opciones disponibles en la ventana de configuración
- **Elegir personaje:** Cambia entre el duende, reno, pingüino, gato o perro en cualquier momento.
- **Cambiar nombre:** Cada mascota muestra su nombre en una etiqueta pequeña debajo de sus pies. Puedes ponerle el nombre que quieras a cada una y darle a *Guardar*.
- **Tamaño de la mascota:** Una barra deslizante (*slider*) que permite achicar o agrandar a la mascota en tiempo real entre `50%` y `180%` (la etiqueta del nombre y la sombra se escalan junto con ella).
- **Iniciar automáticamente con Windows:** Activa o desactiva que el programa arranque solo al iniciar sesión.
- **Mostrar cartel '¡Feliz Navidad!' y nieve al arrancar:** Si lo desmarcas, al encender la PC saldrá directamente la mascota sin la animación grande de 8 segundos.
- **Modo Fantasma:** Vuelve a la mascota semitransparente y hace que todos los clics del ratón la atraviesen para que no estorbe si estás trabajando encima de esa zona. *(Para desactivarlo después, usa el icono junto al reloj de Windows)*.
- **Quedarse fijo en su sitio:** Hace que la mascota deje de caminar por su cuenta. Se quedará exactamente en la coordenada donde la sueltes con el ratón (incluso en medio de la pantalla o en una esquina), pero seguirá haciendo sus animaciones, durmiendo y saltando en su lugar.
- **Desactivar mensajes automáticos:** Evita que la mascota saque globos de texto por su cuenta mientras descansa; solo hablará cuando tú le des clic o la arrastres.

---

## Cómo desactivarlo o desinstalarlo

Si ya pasó Navidad o simplemente no quieres que vuelva a abrirse al encender la computadora, tienes tres opciones:

1. Abrir la ventana de configuración y presionar el botón naranja **"Desactivar y Cerrar"**.
2. O abrir la carpeta `Archivos` y hacer doble clic en **`Desactivar_o_Desinstalar.bat`**.

Ambos métodos cierran el proceso al instante y limpian tanto la clave del Registro (`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`) como el acceso de la carpeta de inicio (`shell:startup`). Después de eso, si quieres borrarlo del todo, basta con eliminar la carpeta del proyecto.

---

## Cómo compilarlo manualmente (si editas el código)

No hace falta instalar Visual Studio. Windows ya trae incluido el compilador de C# de .NET Framework (`csc.exe`). Si modificas `Archivos/CompaneroNavidad.cs`, puedes recompilar el ejecutable abriendo PowerShell en la carpeta del proyecto y ejecutando:

```powershell
& "$env:windir\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /target:winexe /win32icon:"Archivos\assets\icon.ico" /out:"programadenavidadconunnombreinnecesariamentelargo.exe" /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll "Archivos\CompaneroNavidad.cs"
```
