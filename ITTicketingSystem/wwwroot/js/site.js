// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
// NORTHSTAR MOBILE SIDEBAR

document.addEventListener("DOMContentLoaded", function () {
    const sidebar = document.getElementById("northstarSidebar");
    const toggle = document.querySelector(".northstar-menu-toggle");
    const overlay = document.querySelector(".northstar-sidebar-overlay");

    if (!sidebar || !toggle || !overlay) return;

    function closeMenu() {
        sidebar.classList.remove("mobile-open");
        overlay.classList.remove("mobile-open");
        toggle.setAttribute("aria-expanded", "false");
    }

    toggle.addEventListener("click", function () {
        const isOpen = sidebar.classList.toggle("mobile-open");
        overlay.classList.toggle("mobile-open", isOpen);
        toggle.setAttribute("aria-expanded", String(isOpen));
    });

    overlay.addEventListener("click", closeMenu);

    document.addEventListener("keydown", function (event) {
        if (event.key === "Escape") closeMenu();
    });

    sidebar.querySelectorAll("a").forEach(function (link) {
        link.addEventListener("click", closeMenu);
    });

    window.addEventListener("resize", function () {
        if (window.innerWidth > 768) closeMenu();
    });
});