export function setMeta(title, description, keywords, canonical, image, jsonLd) {
  if (title) document.title = title;

  function upsertMeta(selectorAttr, name, content) {
    if (!content) return;
    var selector = selectorAttr === 'name'
      ? 'meta[name="' + name + '"]'
      : 'meta[property="' + name + '"]';
    var el = document.head.querySelector(selector);
    if (el) el.setAttribute('content', content);
    else {
      var m = document.createElement('meta');
      if (selectorAttr === 'name') m.setAttribute('name', name);
      else m.setAttribute('property', name);
      m.setAttribute('content', content);
      document.head.appendChild(m);
    }
  }

  upsertMeta('name', 'description', description);
  upsertMeta('name', 'keywords', keywords);

  upsertMeta('property', 'og:title', title);
  upsertMeta('property', 'og:description', description);
  upsertMeta('property', 'og:image', image);
  upsertMeta('name', 'twitter:card', image ? 'summary_large_image' : 'summary');
  upsertMeta('name', 'twitter:title', title);
  upsertMeta('name', 'twitter:description', description);

  if (canonical) {
    var link = document.head.querySelector('link[rel="canonical"]');
    if (link) link.setAttribute('href', canonical);
    else {
      var l = document.createElement('link');
      l.setAttribute('rel', 'canonical');
      l.setAttribute('href', canonical);
      document.head.appendChild(l);
    }
  }

  if (jsonLd) {
    var id = 'seo-json-ld';
    var existing = document.getElementById(id);
    if (existing) existing.remove();
    var s = document.createElement('script');
    s.type = 'application/ld+json';
    s.id = id;
    s.text = jsonLd;
    document.head.appendChild(s);
  }
}

// Provide a global fallback so older <script> includes still work.
if (typeof window !== "undefined") {
  window.seo = window.seo || {};
  window.seo.setMeta = setMeta;
}