// Жидкое стекло: преломление фона по краям элементов .glass.
// SVG-фильтр внутри backdrop-filter работает только в Chromium,
// в остальных браузерах остаётся обычное размытие из site.css.
(function () {
    const NS = 'http://www.w3.org/2000/svg';
    const glass = document.querySelectorAll('.glass');
    const chromium = !!(navigator.userAgentData && navigator.userAgentData.brands.some(b => b.brand === 'Chromium'));
    const reduced = matchMedia('(prefers-reduced-transparency: reduce)').matches;

    if (chromium && !reduced && glass.length) {
        const svg = document.createElementNS(NS, 'svg');
        svg.setAttribute('width', '0');
        svg.setAttribute('height', '0');
        svg.setAttribute('aria-hidden', 'true');
        svg.style.position = 'absolute';
        document.body.appendChild(svg);

        let next = 0;
        const observer = new ResizeObserver(entries => entries.forEach(e => refract(e.target)));
        glass.forEach(el => observer.observe(el));

        function refract(el) {
            const w = el.offsetWidth, h = el.offsetHeight;
            if (!w || !h || el.dataset.lens === w + 'x' + h) return;
            el.dataset.lens = w + 'x' + h;

            const radius = Math.min(parseFloat(getComputedStyle(el).borderTopLeftRadius) || 0, w / 2, h / 2);
            const blur = el.matches('.sidebar') ? 2 : el.matches('.offcanvas') ? 10 : 6;
            const id = 'lens-' + next++;
            const filter = node(svg, 'filter', { id: id, x: 0, y: 0, width: w, height: h, filterUnits: 'userSpaceOnUse', primitiveUnits: 'userSpaceOnUse', 'color-interpolation-filters': 'sRGB' });
            node(filter, 'feImage', { href: lensMap(w, h, radius), x: 0, y: 0, width: w, height: h, preserveAspectRatio: 'none', result: 'map' });
            node(filter, 'feDisplacementMap', { in: 'SourceGraphic', in2: 'map', scale: 72, xChannelSelector: 'R', yChannelSelector: 'G' });

            el.style.backdropFilter = 'url(#' + id + ') blur(' + blur + 'px) saturate(170%)';
            el.classList.add('lens');
            if (el._lens) el._lens.remove();
            el._lens = filter;
        }
    }

    function node(parent, name, attrs) {
        const n = document.createElementNS(NS, name);
        for (const key in attrs) n.setAttribute(key, attrs[key]);
        return parent.appendChild(n);
    }

    // Карта смещений: в центре ноль, у края фон подтягивается изнутри, как в линзе
    function lensMap(w, h, r) {
        const canvas = document.createElement('canvas');
        canvas.width = w;
        canvas.height = h;
        const ctx = canvas.getContext('2d');
        const img = ctx.createImageData(w, h);
        const bezel = Math.min(28, w / 2, h / 2);
        for (let y = 0, i = 0; y < h; y++) {
            for (let x = 0; x < w; x++, i += 4) {
                const px = x + .5 - w / 2, py = y + .5 - h / 2;
                const qx = Math.abs(px) - (w / 2 - r), qy = Math.abs(py) - (h / 2 - r);
                const ox = Math.max(qx, 0), oy = Math.max(qy, 0), len = Math.hypot(ox, oy);
                const depth = r - len - Math.min(Math.max(qx, qy), 0);
                let nx = 0, ny = 0;
                if (len > 0) { nx = ox / len; ny = oy / len; }
                else if (qx > qy) nx = 1;
                else ny = 1;
                const t = Math.min(Math.max(1 - depth / bezel, 0), 1);
                const m = t * t * 127;
                img.data[i] = 128 - Math.sign(px) * nx * m;
                img.data[i + 1] = 128 - Math.sign(py) * ny * m;
                img.data[i + 2] = 128;
                img.data[i + 3] = 255;
            }
        }
        ctx.putImageData(img, 0, 0);
        return canvas.toDataURL();
    }

    // Блик следует за указателем
    let frame = 0;
    addEventListener('pointermove', function (e) {
        if (frame || e.pointerType === 'touch') return;
        frame = requestAnimationFrame(function () {
            frame = 0;
            const el = e.target.closest && e.target.closest('.glass, .btn-primary');
            if (!el) return;
            const box = el.getBoundingClientRect();
            el.style.setProperty('--mx', (e.clientX - box.left) + 'px');
            el.style.setProperty('--my', (e.clientY - box.top) + 'px');
        });
    }, { passive: true });
})();
