// i18n — TR/EN dil değiştirici. translations.js önce yüklenmeli (global T).
(function () {
  const LANG_KEY = 'dairom_lang';

  function getLang() {
    const saved = localStorage.getItem(LANG_KEY);
    if (saved === 'tr' || saved === 'en') return saved;
    return (navigator.language || 'tr').toLowerCase().startsWith('tr') ? 'tr' : 'en';
  }

  function applyLang(lang) {
    document.documentElement.lang = lang;
    const dict = (window.T && window.T[lang]) || {};
    document.querySelectorAll('[data-i18n]').forEach((el) => {
      const v = dict[el.getAttribute('data-i18n')];
      if (v) el.textContent = v;
    });
    document.querySelectorAll('[data-i18n-html]').forEach((el) => {
      const v = dict[el.getAttribute('data-i18n-html')];
      if (v) el.innerHTML = v;
    });
    document.querySelectorAll('[data-i18n-placeholder]').forEach((el) => {
      const v = dict[el.getAttribute('data-i18n-placeholder')];
      if (v) el.setAttribute('placeholder', v);
    });
    document.querySelectorAll('.lang-btn').forEach((b) =>
      b.classList.toggle('active', b.dataset.lang === lang)
    );
  }

  function setLang(lang) {
    localStorage.setItem(LANG_KEY, lang);
    applyLang(lang);
  }

  document.addEventListener('DOMContentLoaded', () => {
    applyLang(getLang());
    document.querySelectorAll('.lang-btn').forEach((b) =>
      b.addEventListener('click', () => setLang(b.dataset.lang))
    );
  });

  window.__i18n = { getLang, setLang, applyLang };
})();
