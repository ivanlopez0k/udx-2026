// Descarga un archivo que viene de .NET como DotNetStreamReference
// (patrón estándar de Blazor: Blob + <a download> temporal).
window.descargarArchivo = async (nombreArchivo, streamRef) => {
    const arrayBuffer = await streamRef.arrayBuffer();
    const blob = new Blob([arrayBuffer]);
    const url = URL.createObjectURL(blob);

    const enlace = document.createElement("a");
    enlace.href = url;
    enlace.download = nombreArchivo ?? "";
    document.body.appendChild(enlace);
    enlace.click();
    enlace.remove();

    URL.revokeObjectURL(url);
};
