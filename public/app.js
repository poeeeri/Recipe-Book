const state = {
  meta: null,
  allProducts: [],
  products: [],
  dishes: [],
  productFilterFlags: new Set(),
  dishFilterFlags: new Set(),
  productPhotos: [],
  dishPhotos: [],
};

const productForm = document.querySelector("#product-form");
const dishForm = document.querySelector("#dish-form");
const productList = document.querySelector("#product-list");
const dishList = document.querySelector("#dish-list");
const dishItems = document.querySelector("#dish-items");
const dishItemTemplate = document.querySelector("#dish-item-template");
const toast = document.querySelector("#toast");
const dishDraftText = document.querySelector("#dish-draft-text");
const productPhotosInput = document.querySelector("#product-photos-input");
const dishPhotosInput = document.querySelector("#dish-photos-input");
const productPhotosPreview = document.querySelector("#product-photos-preview");
const dishPhotosPreview = document.querySelector("#dish-photos-preview");
const productFlagsContainer = document.querySelector("#product-flags");
const dishFlagsContainer = document.querySelector("#dish-flags");
const detailsModal = document.querySelector("#details-modal");
const detailsModalTitle = document.querySelector("#details-modal-title");
const detailsModalBody = document.querySelector("#details-modal-body");
const closeDetailsModalButton = document.querySelector("#close-details-modal");

function showToast(message, tone = "success") {
  toast.textContent = message;
  toast.className = `toast ${tone}`;
  setTimeout(() => {
    toast.className = "toast hidden";
  }, 3500);
}

async function api(url, options) {
  const response = await fetch(url, {
    headers: { "Content-Type": "application/json" },
    ...options,
  });

  const data = await response.json();
  if (!response.ok) {
    throw data;
  }

  return data;
}

async function filesToDataUrls(fileList) {
  const files = [...fileList];
  return Promise.all(
    files.map(
      (file) =>
        new Promise((resolve, reject) => {
          const reader = new FileReader();
          reader.onload = () => resolve(String(reader.result));
          reader.onerror = reject;
          reader.readAsDataURL(file);
        }),
    ),
  );
}

function setSelectOptions(select, options, emptyLabel) {
  select.innerHTML = "";
  if (emptyLabel) {
    const option = document.createElement("option");
    option.value = "";
    option.textContent = emptyLabel;
    select.append(option);
  }
  for (const value of options) {
    const option = document.createElement("option");
    option.value = value;
    option.textContent = value;
    select.append(option);
  }
}

function formFlags(container) {
  return [...container.querySelectorAll('input[type="checkbox"]:checked')].map((input) => input.value);
}

function setCheckedFlags(container, flags) {
  const values = new Set(flags);
  container.querySelectorAll('input[type="checkbox"]').forEach((input) => {
    input.checked = values.has(input.value);
  });
}

function renderCheckboxes(container, flags, { disabledFlags = [], checkedFlags = [] } = {}) {
  const disabledSet = new Set(disabledFlags);
  const checkedSet = new Set(checkedFlags);
  container.innerHTML = "";
  for (const flag of flags) {
    const label = document.createElement("label");
    if (disabledSet.has(flag)) {
      label.classList.add("disabled");
    }
    label.innerHTML = `
      <input type="checkbox" value="${flag}" ${disabledSet.has(flag) ? "disabled" : ""} ${checkedSet.has(flag) ? "checked" : ""} />
      <span>${flag}</span>
    `;
    container.append(label);
  }
}

function renderFilterFlags(container, flags, stateSet, refresh) {
  container.innerHTML = "";
  for (const flag of flags) {
    const label = document.createElement("label");
    label.innerHTML = `
      <input type="checkbox" value="${flag}" ${stateSet.has(flag) ? "checked" : ""} />
      <span>${flag}</span>
    `;
    label.querySelector("input").addEventListener("change", (event) => {
      if (event.target.checked) {
        stateSet.add(flag);
      } else {
        stateSet.delete(flag);
      }
      refresh();
    });
    container.append(label);
  }
}

function renderPhotoPreview(container, photos, onRemove) {
  container.innerHTML = "";
  photos.forEach((photo, index) => {
    const item = document.createElement("div");
    item.className = "chip";
    item.innerHTML = `
      <span>${photo.startsWith("data:image") ? `Фото ${index + 1}` : photo}</span>
      <button type="button" class="ghost-button" data-index="${index}">Убрать</button>
    `;
    item.querySelector("button").addEventListener("click", () => onRemove(index));
    container.append(item);
  });
}

function formatFlags(flags) {
  return flags.length ? flags.join(", ") : "нет";
}

function formatDate(value, emptyText) {
  return value ? new Date(value).toLocaleString("ru-RU") : emptyText;
}

function detailRow(label, value) {
  const row = document.createElement("div");
  row.className = "details-row";

  const title = document.createElement("strong");
  title.textContent = label;

  const text = document.createElement("span");
  text.textContent = value;

  row.append(title, text);
  return row;
}

function renderDetailsPhotos(photos) {
  const section = document.createElement("section");
  section.className = "details-section";

  const title = document.createElement("h3");
  title.textContent = "Фотографии";
  section.append(title);

  if (!photos.length) {
    const empty = document.createElement("p");
    empty.className = "details-empty";
    empty.textContent = "Фотографии не добавлены.";
    section.append(empty);
    return section;
  }

  const gallery = document.createElement("div");
  gallery.className = "photo-gallery";

  photos.forEach((photo, index) => {
    const item = document.createElement("a");
    item.className = "photo-tile";
    item.href = photo;
    item.target = "_blank";
    item.rel = "noreferrer";

    if (photo.startsWith("data:image") || /\.(png|jpe?g|gif|webp|bmp|svg)$/i.test(photo)) {
      const image = document.createElement("img");
      image.src = photo;
      image.alt = `Фото ${index + 1}`;
      item.append(image);
    } else {
      item.textContent = `Фото ${index + 1}`;
    }

    gallery.append(item);
  });

  section.append(gallery);
  return section;
}

function openDetailsModal(title, content) {
  detailsModalTitle.textContent = title;
  detailsModalBody.replaceChildren(content);
  detailsModal.classList.remove("hidden");
  document.body.classList.add("modal-open");
  closeDetailsModalButton.focus();
}

function closeDetailsModal() {
  detailsModal.classList.add("hidden");
  document.body.classList.remove("modal-open");
  detailsModalBody.replaceChildren();
}

function openProductDetails(product) {
  const content = document.createElement("div");
  content.className = "details-grid";
  content.append(
    renderDetailsPhotos(product.photos),
    detailRow("Название", product.name),
    detailRow("Категория", product.category),
    detailRow("Готовность", product.cookingState),
    detailRow("КБЖУ на 100 г", `${product.calories} / ${product.proteins} / ${product.fats} / ${product.carbs}`),
    detailRow("Флаги", formatFlags(product.flags)),
    detailRow("Состав", product.composition ?? "не указан"),
    detailRow("Создан", formatDate(product.createdAt, "не указано")),
    detailRow("Изменён", formatDate(product.updatedAt, "не изменялся")),
  );

  openDetailsModal(product.name, content);
}

function openDishDetails(dish) {
  const content = document.createElement("div");
  content.className = "details-grid";
  const composition = dish.items.map((item) => `${item.product.name} (${item.quantity} г)`).join(", ");
  content.append(
    renderDetailsPhotos(dish.photos),
    detailRow("Название", dish.name),
    detailRow("Категория", dish.category),
    detailRow("Порция", `${dish.portionSize} г`),
    detailRow("КБЖУ на порцию", `${dish.calories} / ${dish.proteins} / ${dish.fats} / ${dish.carbs}`),
    detailRow("Черновой расчёт", `${dish.nutritionDraft.calories} / ${dish.nutritionDraft.proteins} / ${dish.nutritionDraft.fats} / ${dish.nutritionDraft.carbs}`),
    detailRow("Флаги", formatFlags(dish.flags)),
    detailRow("Состав", composition || "не указан"),
    detailRow("Создано", formatDate(dish.createdAt, "не указано")),
    detailRow("Изменено", formatDate(dish.updatedAt, "не изменялось")),
  );

  openDetailsModal(dish.name, content);
}

function fillMeta() {
  setSelectOptions(productForm.category, state.meta.productCategories, "Выберите категорию");
  setSelectOptions(productForm.cookingState, state.meta.productCookingStates, "Выберите готовность");
  setSelectOptions(dishForm.category, state.meta.dishCategories, "Выберите категорию");
  setSelectOptions(document.querySelector("#product-filter-category"), state.meta.productCategories, "Все категории");
  setSelectOptions(document.querySelector("#product-filter-cooking"), state.meta.productCookingStates, "Любая готовность");
  setSelectOptions(document.querySelector("#dish-filter-category"), state.meta.dishCategories, "Все категории");

  renderCheckboxes(productFlagsContainer, state.meta.flags);
  renderCheckboxes(dishFlagsContainer, state.meta.flags);
  renderFilterFlags(document.querySelector("#product-filter-flags"), state.meta.flags, state.productFilterFlags, loadProducts);
  renderFilterFlags(document.querySelector("#dish-filter-flags"), state.meta.flags, state.dishFilterFlags, loadDishes);
}

function getProductPayload() {
  return {
    name: productForm.name.value,
    photos: state.productPhotos,
    calories: productForm.calories.value,
    proteins: productForm.proteins.value,
    fats: productForm.fats.value,
    carbs: productForm.carbs.value,
    composition: productForm.composition.value.trim(),
    category: productForm.category.value,
    cookingState: productForm.cookingState.value,
    flags: formFlags(productFlagsContainer),
  };
}

function makeDishItemRow(item = {}) {
  const fragment = dishItemTemplate.content.cloneNode(true);
  const row = fragment.querySelector(".dish-item-row");
  const select = row.querySelector('select[name="productId"]');
  setSelectOptions(select, state.allProducts.map((product) => product.name), "Выберите продукт");

  if (item.productId) {
    const product = state.allProducts.find((entry) => entry.id === item.productId);
    if (product) {
      select.value = product.name;
    }
  }

  row.querySelector('input[name="quantity"]').value = item.quantity ?? "";
  row.querySelector(".remove-item").addEventListener("click", () => {
    row.remove();
    updateDishDraft();
  });
  select.addEventListener("change", updateDishDraft);
  row.querySelector('input[name="quantity"]').addEventListener("input", updateDishDraft);
  dishItems.append(fragment);
}

function currentDishItems() {
  return [...dishItems.querySelectorAll(".dish-item-row")]
    .map((row) => {
      const productName = row.querySelector('select[name="productId"]').value;
      const product = state.allProducts.find((entry) => entry.name === productName);
      return {
        productId: product?.id ?? "",
        quantity: Number(row.querySelector('input[name="quantity"]').value),
      };
    })
    .filter((item) => item.productId && item.quantity > 0);
}

function calculateDraft(items) {
  return items.reduce(
    (acc, item) => {
      const product = state.allProducts.find((entry) => entry.id === item.productId);
      if (!product) {
        return acc;
      }
      const ratio = item.quantity / 100;
      acc.calories += product.calories * ratio;
      acc.proteins += product.proteins * ratio;
      acc.fats += product.fats * ratio;
      acc.carbs += product.carbs * ratio;
      return acc;
    },
    { calories: 0, proteins: 0, fats: 0, carbs: 0 },
  );
}

function clearManualDishNutrition() {
  delete dishForm.dataset.manualValues;
}

function hasManualDishNutrition() {
  return dishForm.dataset.manualValues === "true";
}

function availableDishFlags(items) {
  if (!items.length) {
    return [];
  }

  return state.meta.flags.filter((flag) =>
    items.every((item) => {
      const product = state.allProducts.find((entry) => entry.id === item.productId);
      return product?.flags.includes(flag);
    }),
  );
}

function updateDishDraft() {
  const items = currentDishItems();
  const previousCheckedFlags = formFlags(dishFlagsContainer);
  const draft = calculateDraft(items);
  dishDraftText.textContent = `Калории: ${draft.calories.toFixed(2)}, Б: ${draft.proteins.toFixed(2)}, Ж: ${draft.fats.toFixed(2)}, У: ${draft.carbs.toFixed(2)}`;

  if (!hasManualDishNutrition()) {
    dishForm.calories.value = draft.calories.toFixed(2);
    dishForm.proteins.value = draft.proteins.toFixed(2);
    dishForm.fats.value = draft.fats.toFixed(2);
    dishForm.carbs.value = draft.carbs.toFixed(2);
  }

  const availableFlags = availableDishFlags(items);
  renderCheckboxes(dishFlagsContainer, state.meta.flags, {
    disabledFlags: state.meta.flags.filter((flag) => !availableFlags.includes(flag)),
    checkedFlags: previousCheckedFlags.filter((flag) => availableFlags.includes(flag)),
  });
}

function getDishPayload() {
  return {
    name: dishForm.name.value,
    photos: state.dishPhotos,
    category: dishForm.category.value,
    portionSize: dishForm.portionSize.value,
    calories: dishForm.calories.value,
    proteins: dishForm.proteins.value,
    fats: dishForm.fats.value,
    carbs: dishForm.carbs.value,
    flags: formFlags(dishFlagsContainer),
    items: currentDishItems(),
  };
}

function resetProductForm() {
  productForm.reset();
  productForm.id.value = "";
  state.productPhotos = [];
  renderPhotoPreview(productPhotosPreview, state.productPhotos, (index) => {
    state.productPhotos.splice(index, 1);
    renderPhotoPreview(productPhotosPreview, state.productPhotos, removeProductPhoto);
  });
  renderCheckboxes(productFlagsContainer, state.meta.flags);
}

function resetDishForm() {
  dishForm.reset();
  dishForm.id.value = "";
  clearManualDishNutrition();
  state.dishPhotos = [];
  renderPhotoPreview(dishPhotosPreview, state.dishPhotos, (index) => {
    state.dishPhotos.splice(index, 1);
    renderPhotoPreview(dishPhotosPreview, state.dishPhotos, removeDishPhoto);
  });
  dishItems.innerHTML = "";
  makeDishItemRow();
  renderCheckboxes(dishFlagsContainer, state.meta.flags, { disabledFlags: state.meta.flags });
  dishDraftText.textContent = "Добавьте продукты в состав блюда.";
}

function removeProductPhoto(index) {
  state.productPhotos.splice(index, 1);
  renderPhotoPreview(productPhotosPreview, state.productPhotos, removeProductPhoto);
}

function removeDishPhoto(index) {
  state.dishPhotos.splice(index, 1);
  renderPhotoPreview(dishPhotosPreview, state.dishPhotos, removeDishPhoto);
}

function productCard(product) {
  const card = document.createElement("article");
  card.className = "card";
  card.innerHTML = `
    <h3>${product.name}</h3>
    <div class="meta">Категория: ${product.category}</div>
    <div class="meta">Готовность: ${product.cookingState}</div>
    <div class="meta">КБЖУ: ${product.calories} / ${product.proteins} / ${product.fats} / ${product.carbs}</div>
    <div class="meta">Фотографий: ${product.photos.length}</div>
    <div class="card-actions">
      <button type="button" class="ghost-button" data-view="${product.id}">Подробнее</button>
      <button type="button" data-edit="${product.id}">Редактировать</button>
      <button type="button" data-delete="${product.id}">Удалить</button>
    </div>
  `;

  card.querySelector("[data-view]").addEventListener("click", () => openProductDetails(product));

  card.querySelector("[data-edit]").addEventListener("click", () => {
    productForm.id.value = product.id;
    productForm.name.value = product.name;
    productForm.calories.value = product.calories;
    productForm.proteins.value = product.proteins;
    productForm.fats.value = product.fats;
    productForm.carbs.value = product.carbs;
    productForm.composition.value = product.composition ?? "";
    productForm.category.value = product.category;
    productForm.cookingState.value = product.cookingState;
    state.productPhotos = [...product.photos];
    renderPhotoPreview(productPhotosPreview, state.productPhotos, removeProductPhoto);
    setCheckedFlags(productFlagsContainer, product.flags);
    productForm.scrollIntoView({ behavior: "smooth", block: "start" });
  });

  card.querySelector("[data-delete]").addEventListener("click", async () => {
    try {
      await api(`/api/products/${product.id}`, { method: "DELETE" });
      showToast("Продукт удалён.");
      await Promise.all([loadProducts(), loadDishes()]);
    } catch (error) {
      const dishes = (error.dishes ?? []).map((dish) => dish.name).join(", ");
      showToast(`${error.error}${dishes ? ` Используется в: ${dishes}` : ""}`, "error");
    }
  });

  return card;
}

function dishCard(dish) {
  const card = document.createElement("article");
  card.className = "card";
  card.innerHTML = `
    <h3>${dish.name}</h3>
    <div class="meta">Категория: ${dish.category}</div>
    <div class="meta">Порция: ${dish.portionSize} г</div>
    <div class="meta">КБЖУ на порцию: ${dish.calories} / ${dish.proteins} / ${dish.fats} / ${dish.carbs}</div>
    <div class="meta">Фотографий: ${dish.photos.length}</div>
    <div class="card-actions">
      <button type="button" class="ghost-button" data-view="${dish.id}">Подробнее</button>
      <button type="button" data-edit="${dish.id}">Редактировать</button>
      <button type="button" data-delete="${dish.id}">Удалить</button>
    </div>
  `;

  card.querySelector("[data-view]").addEventListener("click", () => openDishDetails(dish));

  card.querySelector("[data-edit]").addEventListener("click", () => {
    dishForm.id.value = dish.id;
    dishForm.name.value = dish.name;
    dishForm.category.value = dish.category;
    dishForm.portionSize.value = dish.portionSize;
    dishForm.calories.value = dish.calories;
    dishForm.proteins.value = dish.proteins;
    dishForm.fats.value = dish.fats;
    dishForm.carbs.value = dish.carbs;
    dishForm.dataset.manualValues = "true";
    state.dishPhotos = [...dish.photos];
    renderPhotoPreview(dishPhotosPreview, state.dishPhotos, removeDishPhoto);
    dishItems.innerHTML = "";
    dish.items.forEach((item) => makeDishItemRow(item));
    updateDishDraft();
    setCheckedFlags(dishFlagsContainer, dish.flags);
    dishForm.scrollIntoView({ behavior: "smooth", block: "start" });
  });

  card.querySelector("[data-delete]").addEventListener("click", async () => {
    try {
      await api(`/api/dishes/${dish.id}`, { method: "DELETE" });
      showToast("Блюдо удалено.");
      await loadDishes();
    } catch (error) {
      showToast(error.error ?? "Не удалось удалить блюдо.", "error");
    }
  });

  return card;
}

async function loadProducts() {
  const allProducts = await api("/api/products");
  state.allProducts = allProducts;

  const params = new URLSearchParams();
  const search = document.querySelector("#product-search").value.trim();
  const category = document.querySelector("#product-filter-category").value;
  const cookingState = document.querySelector("#product-filter-cooking").value;
  const sort = document.querySelector("#product-sort").value;
  const order = document.querySelector("#product-order").value;

  if (search) params.set("search", search);
  if (category) params.set("category", category);
  if (cookingState) params.set("cookingState", cookingState);
  if (sort) params.set("sort", sort);
  if (order) params.set("order", order);
  for (const flag of state.productFilterFlags) {
    params.append("flags", flag);
  }

  state.products = await api(`/api/products?${params.toString()}`);
  productList.replaceChildren(...state.products.map(productCard));
}

async function loadDishes() {
  const params = new URLSearchParams();
  const search = document.querySelector("#dish-search").value.trim();
  const category = document.querySelector("#dish-filter-category").value;

  if (search) params.set("search", search);
  if (category) params.set("category", category);
  for (const flag of state.dishFilterFlags) {
    params.append("flags", flag);
  }

  state.dishes = await api(`/api/dishes?${params.toString()}`);
  dishList.replaceChildren(...state.dishes.map(dishCard));
}

productForm.addEventListener("submit", async (event) => {
  event.preventDefault();
  try {
    const id = productForm.id.value;
    const method = id ? "PUT" : "POST";
    const url = id ? `/api/products/${id}` : "/api/products";
    await api(url, { method, body: JSON.stringify(getProductPayload()) });
    showToast(id ? "Продукт обновлён." : "Продукт создан.");
    resetProductForm();
    await Promise.all([loadProducts(), loadDishes()]);
  } catch (error) {
    showToast(error.error ?? "Ошибка сохранения продукта.", "error");
  }
});

dishForm.addEventListener("submit", async (event) => {
  event.preventDefault();
  try {
    const id = dishForm.id.value;
    const method = id ? "PUT" : "POST";
    const url = id ? `/api/dishes/${id}` : "/api/dishes";
    await api(url, { method, body: JSON.stringify(getDishPayload()) });
    showToast(id ? "Блюдо обновлено." : "Блюдо создано.");
    resetDishForm();
    await loadDishes();
  } catch (error) {
    showToast(error.error ?? "Ошибка сохранения блюда.", "error");
  }
});

["calories", "proteins", "fats", "carbs"].forEach((name) => {
  dishForm[name].addEventListener("input", () => {
    dishForm.dataset.manualValues = "true";
  });
});

productPhotosInput.addEventListener("change", async () => {
  const loadedPhotos = await filesToDataUrls(productPhotosInput.files);
  state.productPhotos = [...state.productPhotos, ...loadedPhotos].slice(0, 5);
  renderPhotoPreview(productPhotosPreview, state.productPhotos, removeProductPhoto);
  productPhotosInput.value = "";
});

dishPhotosInput.addEventListener("change", async () => {
  const loadedPhotos = await filesToDataUrls(dishPhotosInput.files);
  state.dishPhotos = [...state.dishPhotos, ...loadedPhotos].slice(0, 5);
  renderPhotoPreview(dishPhotosPreview, state.dishPhotos, removeDishPhoto);
  dishPhotosInput.value = "";
});

document.querySelector("#add-dish-item").addEventListener("click", () => {
  makeDishItemRow();
});

document.querySelector("#reset-product-form").addEventListener("click", resetProductForm);
document.querySelector("#reset-dish-form").addEventListener("click", resetDishForm);
closeDetailsModalButton.addEventListener("click", closeDetailsModal);
detailsModal.querySelector("[data-close-details]").addEventListener("click", closeDetailsModal);
document.addEventListener("keydown", (event) => {
  if (event.key === "Escape" && !detailsModal.classList.contains("hidden")) {
    closeDetailsModal();
  }
});

["#product-search", "#product-filter-category", "#product-filter-cooking", "#product-sort", "#product-order"].forEach((selector) => {
  document.querySelector(selector).addEventListener("input", loadProducts);
  document.querySelector(selector).addEventListener("change", loadProducts);
});

["#dish-search", "#dish-filter-category"].forEach((selector) => {
  document.querySelector(selector).addEventListener("input", loadDishes);
  document.querySelector(selector).addEventListener("change", loadDishes);
});

async function bootstrap() {
  state.meta = await api("/api/meta");
  fillMeta();
  resetProductForm();
  resetDishForm();
  await Promise.all([loadProducts(), loadDishes()]);
}

bootstrap().catch((error) => {
  showToast(error.error ?? "Не удалось инициализировать приложение.", "error");
});
