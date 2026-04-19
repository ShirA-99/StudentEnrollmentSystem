document.addEventListener("DOMContentLoaded", () => {
    document.body.classList.add("js-ready");

    const revealTargets = document.querySelectorAll("[data-reveal]");
    const prefersReducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

    if (prefersReducedMotion || !("IntersectionObserver" in window)) {
        revealTargets.forEach((target) => target.classList.add("is-visible"));
        return;
    }

    const observer = new IntersectionObserver((entries) => {
        entries.forEach((entry) => {
            if (!entry.isIntersecting) {
                return;
            }

            const delay = entry.target.getAttribute("data-delay");
            if (delay) {
                entry.target.style.transitionDelay = `${delay}ms`;
            }

            entry.target.classList.add("is-visible");
            observer.unobserve(entry.target);
        });
    }, {
        rootMargin: "0px 0px -10% 0px",
        threshold: 0.18
    });

    revealTargets.forEach((target) => observer.observe(target));
});
