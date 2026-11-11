// NeuCharBox's official accounts: the header's icons and the "Follow NeuCharBox" line of every end card.
const G = (d) => `<svg viewBox="0 0 24 24" fill="currentColor" fill-rule="evenodd" aria-hidden="true"><path d="${d}"/></svg>`;

export const SOCIAL = [
  { id: 'youtube', name: 'YouTube', url: 'https://www.youtube.com/@NeuCharBoxOfficial',
    svg: G('M6.5 4.5h11a4.5 4.5 0 0 1 4.5 4.5v6a4.5 4.5 0 0 1-4.5 4.5h-11A4.5 4.5 0 0 1 2 15V9a4.5 4.5 0 0 1 4.5-4.5zM10 8.6v6.8l5.8-3.4z') },
  { id: 'linkedin', name: 'LinkedIn', url: 'https://www.linkedin.com/company/neucharbox',
    svg: G('M4.5 2h15A2.5 2.5 0 0 1 22 4.5v15a2.5 2.5 0 0 1-2.5 2.5h-15A2.5 2.5 0 0 1 2 19.5v-15A2.5 2.5 0 0 1 4.5 2zM7.4 5.6a1.7 1.7 0 1 0 0 3.4 1.7 1.7 0 1 0 0-3.4zM5.9 10h3v8.5h-3zM10.8 10h2.9v1.2c.5-.8 1.5-1.4 2.8-1.4 2.2 0 3.3 1.4 3.3 4v4.7h-3v-4.3c0-1.2-.4-1.9-1.4-1.9s-1.6.7-1.6 1.9v4.3h-3z') },
  { id: 'x', name: 'X', url: 'https://x.com/NeuCharBox',
    svg: G('M17.8 2.5h3.2l-7 8 8.2 11h-6.4l-5-6.6-5.8 6.6H1.8l7.5-8.6L1.4 2.5h6.6l4.6 6.1zm-1.1 17.1h1.8L7.2 4.3H5.3z') },
];

// One link per account: an icon with the name beside it (`labels`), or the icon alone (the name is its label and tooltip).
export const socialLinks = ({ labels = false } = {}) => SOCIAL.map((s) =>
  `<a class="social-link ${s.id}" href="${s.url}" target="_blank" rel="noopener"${labels ? '' : ` aria-label="NeuCharBox on ${s.name}" title="${s.name}"`}>${s.svg}${labels ? `<span>${s.name}</span>` : ''}</a>`).join('');
