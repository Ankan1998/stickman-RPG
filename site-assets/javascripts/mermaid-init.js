/**
 * Eager Mermaid rendering for the course site.
 *
 * Material for MkDocs has built-in Mermaid support, but it mounts each diagram
 * lazily, when it scrolls into view. That silently produces empty boxes in
 * background or unpainted tabs and when printing, and a failed diagram leaves
 * a blank gap with no explanation. So every diagram is rendered up front here,
 * a failure is printed where the diagram should be, and diagrams re-render
 * when the reader toggles light/dark so their colours follow.
 *
 * The ```mermaid fences are emitted as <div class="mermaid-diagram"> holding
 * the escaped diagram source (see mkdocs.yml -> superfences custom_fences).
 * GitHub renders the very same fences natively, so one source serves both.
 */

import mermaid from "https://cdn.jsdelivr.net/npm/mermaid@11.4.1/dist/mermaid.esm.min.mjs";

const SELECTOR = ".mermaid-diagram";
let counter = 0;

/** Material flags dark mode with data-md-color-scheme="slate" on <body>. */
function currentTheme() {
  return document.body.getAttribute("data-md-color-scheme") === "slate"
    ? "dark"
    : "default";
}

/** Stash the original source so theme switches can re-render from it. */
function cacheSources() {
  for (const el of document.querySelectorAll(SELECTOR)) {
    if (el.dataset.source === undefined) {
      el.dataset.source = el.textContent.trim();
    }
  }
}

async function renderAll() {
  cacheSources();

  mermaid.initialize({
    startOnLoad: false,
    theme: currentTheme(),
    // Diagram sources are authored in this repo, so HTML labels (<br/>) are safe.
    securityLevel: "loose",
    fontFamily: "inherit",
    flowchart: { htmlLabels: true, useMaxWidth: true, curve: "basis" },
    sequence: { useMaxWidth: true, wrap: true },
    state: { useMaxWidth: true },
  });

  for (const el of document.querySelectorAll(SELECTOR)) {
    const source = el.dataset.source;
    if (!source) continue;

    try {
      const { svg } = await mermaid.render(`mermaid-svg-${counter++}`, source);
      el.innerHTML = svg;
      el.classList.remove("mermaid-failed");
      el.classList.add("mermaid-rendered");
    } catch (error) {
      // Surface the failure instead of leaving a blank gap on the page.
      el.classList.add("mermaid-failed");
      el.innerHTML =
        `<strong>Diagram failed to render.</strong><br/>` +
        `<code>${String(error && error.message ? error.message : error)}</code>`;
    }
  }
}

await renderAll();

// Re-render when the reader toggles light/dark so diagram colours follow.
new MutationObserver((mutations) => {
  if (mutations.some((m) => m.attributeName === "data-md-color-scheme")) {
    renderAll();
  }
}).observe(document.body, { attributes: true });
