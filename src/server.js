const express = require('express');
const fs = require('fs');
const path = require('path');

const app = express();
const PORT = process.env.PORT || 3000;
const DATA_FILE = path.join(__dirname, '..', 'data', 'records.json');

app.use(express.json());
app.use(express.urlencoded({ extended: true }));
app.use(express.static(path.join(__dirname, '..', 'public')));

function readData() {
  if (!fs.existsSync(DATA_FILE)) {
    fs.writeFileSync(DATA_FILE, '[]');
    return [];
  }

  return JSON.parse(fs.readFileSync(DATA_FILE, 'utf8'));
}

function writeData(items) {
  fs.writeFileSync(DATA_FILE, JSON.stringify(items, null, 2));
}

app.get('/api/items', (req, res) => {
  res.json(readData());
});

app.post('/api/items', (req, res) => {
  const { name, email, role } = req.body;
  if (!name || !email || !role) {
    return res.status(400).json({ error: 'All fields are required.' });
  }

  const items = readData();
  const newItem = { id: Date.now().toString(), name: name.trim(), email: email.trim(), role: role.trim() };
  items.push(newItem);
  writeData(items);
  res.status(201).json(newItem);
});

app.put('/api/items/:id', (req, res) => {
  const { name, email, role } = req.body;
  if (!name || !email || !role) {
    return res.status(400).json({ error: 'All fields are required.' });
  }

  const items = readData();
  const index = items.findIndex(item => item.id === req.params.id);
  if (index === -1) {
    return res.status(404).json({ error: 'Item not found.' });
  }

  items[index] = { ...items[index], name: name.trim(), email: email.trim(), role: role.trim() };
  writeData(items);
  res.json(items[index]);
});

app.delete('/api/items/:id', (req, res) => {
  const items = readData();
  const filtered = items.filter(item => item.id !== req.params.id);
  if (filtered.length === items.length) {
    return res.status(404).json({ error: 'Item not found.' });
  }

  writeData(filtered);
  res.json({ success: true });
});

app.get('*', (req, res) => {
  res.sendFile(path.join(__dirname, '..', 'public', 'index.html'));
});

app.listen(PORT, () => {
  console.log(`Server running at http://localhost:${PORT}`);
});
