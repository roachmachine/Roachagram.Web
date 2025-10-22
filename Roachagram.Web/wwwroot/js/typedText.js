window.roachagram = {
  startTypewriter: function (elementId, text, speed) {
    const el = document.getElementById(elementId);
    if (!el) return;

    // Clear target
    el.innerHTML = '';
    el.textContent = '';

    let i = 0;
    function tick() {
      if (i < text.length) {
        // Append as textContent so partial HTML tags are not parsed
        el.textContent += text.charAt(i++);
        setTimeout(tick, speed);
      } else {
        // After typing finishes, render the final HTML
        el.innerHTML = text;
      }
    }
    tick();
  }
};