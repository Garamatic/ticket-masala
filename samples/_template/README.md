# Sample Template

This is the base template for creating new TicketMasala sample instances.

## Usage

1. Copy this entire directory to create a new sample:
   ```bash
   cp -r _template/ my-new-sample/
   ```

2. Rename `config.json.template` to `config.json` and fill in your values

3. Customize `style.css` with your theme colors

4. Add your assets to the `assets/` directory

5. Test locally:
   ```bash
   python3 -m http.server 8000
   ```

## Files

| File | Purpose |
|------|---------|
| `config.json.template` | Configuration template - rename to `config.json` |
| `index.html` | Landing page structure |
| `style.css` | Base styles with CSS variable theming |
| `main.js` | Shared JavaScript logic |
| `assets/` | Images, thumbnails, and media |

## Configuration

See `config.json.template` for the full structure. Key sections:

- **client_config**: Branding, services, projects
- **masala_config**: Backend API settings
- **gerda_config**: Validation and workflow rules

## Theme Colors

Update the CSS variables in `style.css`:

```css
:root {
    --primary: #YOUR_COLOR;
    --primary-dark: #DARKER_VARIANT;
    --primary-light: #LIGHTER_VARIANT;
    --accent: #ACCENT_COLOR;
}
```

The theme colors from `config.json` will be applied automatically via JavaScript.
