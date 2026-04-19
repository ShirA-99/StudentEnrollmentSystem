document.addEventListener("DOMContentLoaded", () => {
    document.body.classList.add("js-ready");

    const revealTargets = document.querySelectorAll("[data-reveal]");
    const prefersReducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    const profileTabList = document.querySelector("[data-profile-tabs]");

    if (profileTabList) {
        const profileTabButtons = Array.from(profileTabList.querySelectorAll("[data-profile-tab]"));
        const profilePanels = Array.from(document.querySelectorAll("[data-profile-panel]"));

        const setActiveProfileSection = (sectionName, updateHistory = true) => {
            profileTabButtons.forEach((button) => {
                const isActive = button.dataset.profileTab === sectionName;
                button.classList.toggle("is-active", isActive);
                button.setAttribute("aria-selected", isActive ? "true" : "false");
                button.tabIndex = isActive ? 0 : -1;
            });

            profilePanels.forEach((panel) => {
                const isActive = panel.dataset.profilePanel === sectionName;
                panel.classList.toggle("is-active", isActive);
                panel.hidden = !isActive;
            });

            profileTabList.dataset.activeSection = sectionName;

            if (updateHistory) {
                const nextUrl = new URL(window.location.href);
                nextUrl.searchParams.set("section", sectionName);
                window.history.replaceState({}, "", nextUrl);
            }
        };

        const initialProfileSection = profileTabList.dataset.activeSection || "profile";
        setActiveProfileSection(initialProfileSection, false);

        profileTabButtons.forEach((button) => {
            button.addEventListener("click", () => {
                const nextSection = button.dataset.profileTab;
                if (!nextSection) {
                    return;
                }

                setActiveProfileSection(nextSection);
            });

            button.addEventListener("keydown", (event) => {
                const currentIndex = profileTabButtons.indexOf(button);
                if (currentIndex === -1) {
                    return;
                }

                if (event.key !== "ArrowDown" && event.key !== "ArrowRight" && event.key !== "ArrowUp" && event.key !== "ArrowLeft") {
                    return;
                }

                event.preventDefault();

                const movingForward = event.key === "ArrowDown" || event.key === "ArrowRight";
                const nextIndex = movingForward
                    ? (currentIndex + 1) % profileTabButtons.length
                    : (currentIndex - 1 + profileTabButtons.length) % profileTabButtons.length;

                profileTabButtons[nextIndex].focus();
                profileTabButtons[nextIndex].click();
            });
        });
    }

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
