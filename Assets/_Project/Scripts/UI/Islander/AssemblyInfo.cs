using System.Runtime.CompilerServices;

// La prueba de la ficha vive en Nimbo.PlayTests y necesita clavar el contrato del
// aviso («lo que digo aguanta los refrescos») sin simular clics de verdad. Solo se
// abre lo mínimo: el método que escribe el aviso, no la construcción de botones.
[assembly: InternalsVisibleTo("Nimbo.PlayTests")]
