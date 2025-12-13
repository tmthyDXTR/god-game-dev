#!/usr/bin/env node
const fs = require('fs');
const path = require('path');

// This script works when the repository root is either the project root
// (where `Assets/docs` exists) or the `Assets/` folder itself (where `docs` exists).
const candidates = [
  path.join(process.cwd(), 'Assets', 'docs'),
  path.join(process.cwd(), 'docs')
];

let docsDir = null;
for (const c of candidates) {
  if (fs.existsSync(c) && fs.statSync(c).isDirectory()) {
    docsDir = c;
    break;
  }
}

if (!docsDir) {
  console.error('Docs directory not found. Checked:', candidates.join(', '));
  process.exit(1);
}

const files = fs.readdirSync(docsDir).filter(f => f.endsWith('.md')).sort();
if (files.length === 0) {
  console.log('No markdown files found in', docsDir);
  process.exit(0);
}

function humanize(name) {
//   if (name === 'index') return 'Home';
  return name.replace(/[-_]/g, ' ').replace(/\b\w/g, c => c.toUpperCase());
}

const navItems = files.map(f => {
  const name = path.basename(f, '.md');
  return { title: humanize(name), href: name + '.html' };
});

const startMarker = '<!-- AUTO_NAV_START -->';
const endMarker = '<!-- AUTO_NAV_END -->';

const navLines = [];
navLines.push(startMarker);
navLines.push('<nav>');
navLines.push('<ul style="list-style:none; padding:0; display:flex; gap:1rem;">');
navItems.forEach(item => {
  navLines.push(`  <li><a href="${item.href}">${item.title}</a></li>`);
});
navLines.push('</ul>');
navLines.push('</nav>');
navLines.push(endMarker);
const navBlock = navLines.join('\n');

files.forEach(file => {
  const filePath = path.join(docsDir, file);
  let content = fs.readFileSync(filePath, 'utf8');
  if (content.includes(startMarker) && content.includes(endMarker)) {
    const regex = new RegExp(`${startMarker}[\s\S]*?${endMarker}`, 'm');
    content = content.replace(regex, navBlock);
    fs.writeFileSync(filePath, content, 'utf8');
    console.log('Updated nav in', file);
  } else {
    content = navBlock + '\n\n' + content;
    fs.writeFileSync(filePath, content, 'utf8');
    console.log('Inserted nav in', file);
  }
});

console.log('Docs nav generation complete at', docsDir);
