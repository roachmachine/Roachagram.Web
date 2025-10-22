// Add a module-scoped controllers Map to track running animations
const controllers = new Map();

export function startTypewriter(elementId, html, delay = 40) {
  if (!elementId) {
    console.warn('startTypewriter: missing elementId');
    return;
  }

  const el = document.getElementById(elementId);
  if (!el) {
    console.warn(`startTypewriter: element with id "${elementId}" not found`);
    return;
  }

  // Cancel existing animation for this element
  if (controllers.has(elementId)) {
    controllers.get(elementId).cancel = true;
    controllers.delete(elementId);
  }

  // Controller object to allow cancellation
  const controller = { cancel: false };
  controllers.set(elementId, controller);

  // Clear element content
  el.innerHTML = '';

  // Async loop to append HTML while treating tags as atomic
  (async () => {
    try {
      const text = html ?? '';
      let i = 0;
      const len = text.length;
      while (i < len) {
        if (controller.cancel) break;

        if (text[i] === '<') {
          // collect until next '>' (inclusive) and append whole tag
          let j = i;
          while (j < len && text[j] !== '>') j++;
          // if no closing '>', append rest and break
          const tag = text.slice(i, Math.min(j + 1, len));
          el.innerHTML += tag;
          i = j + 1;
        } else {
          // append a single character
          const char = text[i];
          const span = document.createElement('span');
          span.textContent = char;
          el.appendChild(span);
          i++;
        }

        // Wait
        await new Promise((res) => setTimeout(res, delay));
      }
    } catch (err) {
      console.error('startTypewriter error', err);
    } finally {
      // cleanup controller
      controllers.delete(elementId);
    }
  })();
}

export function stopTypewriter(elementId) {
  if (!elementId) return;
  const c = controllers.get(elementId);
  if (c) {
    c.cancel = true;
    controllers.delete(elementId);
  }
}