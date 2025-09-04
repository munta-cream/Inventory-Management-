// Drag-and-drop ID format builder
const el = document.getElementById('id-format-builder');
Sortable.create(el, {
    animation: 150,
    onEnd: updatePreview
});

function updatePreview() {
    const format = [];
    document.querySelectorAll('#id-format-builder .component')?.forEach(c => {
        format.push(JSON.parse(c.dataset.component));
    });
    fetch('/CustomId/Preview', {
        method: 'POST',
        body: JSON.stringify({ inventoryId: 1, format: format }),
        headers: { 'Content-Type': 'application/json' }
    }).then(r => r.json()).then(data => {
        document.getElementById('preview').innerText = data.id;
    });
}