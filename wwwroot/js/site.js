// ============================================================
//  TrendyKart storefront JS  (plain JavaScript, no libraries)
// ============================================================

// ---- Add to cart / update quantity (works for any <form class="ajax-cart-form">) ----
document.addEventListener("submit", async function (e) {
    if (!e.target.classList.contains("ajax-cart-form")) return;

    e.preventDefault();
    const form = e.target;
    const url = form.getAttribute("data-url");

    try {
        const response = await fetch(url, { method: "POST", body: new FormData(form) });

        // Not logged in -> the cart endpoints require login, so send them there.
        if (response.status === 401 || response.redirected) {
            window.location.href = "/Account/Login";
            return;
        }

        if (response.ok) {
            window.location.reload();
        } else {
            alert("Something went wrong. Please try again.");
        }
    } catch (err) {
        console.error("Cart error:", err);
    }
});

// ---- Product tabs (New Arrival / Bestseller / Featured) ----
function showTab(event, panelId) {
    document.querySelectorAll(".tab-panel").forEach(p => p.classList.remove("active"));
    document.querySelectorAll(".tab-btn").forEach(b => b.classList.remove("active"));
    document.getElementById(panelId).classList.add("active");
    event.currentTarget.classList.add("active");
}

// ---- Category chips horizontal scroll (arrow buttons) ----
function scrollChips(direction) {
    const row = document.getElementById("chips");
    if (row) row.scrollBy({ left: direction * 260, behavior: "smooth" });
}

// ---- Wishlist Card Toggle ----
async function toggleWishlistCard(productId, btnEl) {
    try {
        const formData = new FormData();
        formData.append("productId", productId);

        const response = await fetch("/Wishlist/ToggleWishlist", {
            method: "POST",
            body: formData
        });

        if (response.status === 401 || response.redirected) {
            window.location.href = "/Account/Login";
            return;
        }

        const data = await response.json();
        if (data.success) {
            const icon = btnEl.querySelector("i");
            if (icon) {
                if (data.added) {
                    icon.className = "fas fa-heart text-danger";
                } else {
                    icon.className = "far fa-heart text-secondary";
                }
            }
        } else if (data.requireLogin) {
            window.location.href = "/Account/Login";
        } else {
            alert(data.message || "Unable to update wishlist.");
        }
    } catch (err) {
        console.error("Wishlist error:", err);
    }
}

