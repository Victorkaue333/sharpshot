# Cores — Design system

> Paleta do SharpShot, papel de cada cor, tokens XAML (convenção) e contraste WCAG calculado. Fonte executável: constantes em [`tools/branding/generate_branding.py`](../../tools/branding/generate_branding.py). Valores por tema em [`temas.md`](temas.md).
> Última atualização: 04/nov/2026 · Mantenedor: Victor Alves

## Paleta

| Nome | Hex | RGB | Papel | Token XAML (convenção) | No script |
|------|-----|-----|-------|------------------------|-----------|
| **Background** | `#0D1017` | 13, 16, 23 | Fundo de janela e do overlay (máscara); texto sobre o botão primário | `SharpShotBackgroundBrush` | `BACKGROUND` |
| **Surface** | `#1B2230` | 27, 34, 48 | Cards, painel do `NavigationView`, barra de título, flyouts | `SharpShotSurfaceBrush` | `SURFACE` |
| **Primary** | `#FFAA35` | 255, 170, 53 | Botão primário (preenchimento), borda da seleção, handles, anel de foco, acento do logo | `SharpShotPrimaryBrush` | `PRIMARY` |
| **Primary hover** | `#FF7A00` | 255, 122, 0 | Hover/pressed do primário; destaque forte | `SharpShotPrimaryHoverBrush` | `PRIMARY_HOVER` |
| **Text** | `#F5F7FA` | 245, 247, 250 | Texto principal (escuro); prata clara do logo | `SharpShotTextBrush` | `TEXT_PRIMARY` / `SILVER_LIGHT` |
| **Text secondary** | `#94A3B8` | 148, 163, 184 | Texto secundário, legendas, ícones inativos (escuro); prata sobre fundo claro no logo | `SharpShotTextSecondaryBrush` | `TEXT_SECONDARY` / `SILVER_ON_LIGHT_A` |
| Silver dark *(só logo)* | `#C9D1DC` | 201, 209, 220 | Ponta escura do gradiente prata do logo | — | `SILVER_DARK` |
| Silver on light B *(só logo)* | `#5B6B80` | 91, 107, 128 | Gradiente prata sobre fundo claro | — | `SILVER_ON_LIGHT_B` |
| Tile border *(só ícone)* | `#2A3447` | 42, 52, 71 | Borda do tile do ícone do app | — | `TILE_BORDER` |

As três últimas pertencem ao logo/ícone e **não** entram na UI.

## Luminância e contraste (WCAG 2.x)

Luminância relativa calculada pela fórmula sRGB; contraste = (L1 + 0,05) / (L2 + 0,05). AA: 4,5:1 texto normal, 3:1 texto grande e componentes de UI.

| Cor | Luminância |
|-----|------------|
| `#0D1017` | 0,0052 |
| `#1B2230` | 0,0159 |
| `#94A3B8` | 0,360 |
| `#FFAA35` | 0,503 |
| `#FF7A00` | 0,352 |
| `#F5F7FA` | 0,928 |
| `#FFFFFF` | 1,000 |

| Primeiro plano | Fundo | Contraste | Veredito |
|----------------|-------|-----------|----------|
| `#F5F7FA` | `#0D1017` | **17,7:1** | AAA |
| `#F5F7FA` | `#1B2230` | **14,8:1** | AAA |
| `#94A3B8` | `#0D1017` | **7,4:1** | AAA |
| `#94A3B8` | `#1B2230` | **6,2:1** | AA (texto normal) |
| `#FFAA35` | `#0D1017` | **10,0:1** | AAA — texto laranja no escuro é legível |
| `#FFAA35` | `#1B2230` | **8,4:1** | AAA |
| `#FF7A00` | `#0D1017` | **7,3:1** | AAA |
| `#0D1017` | `#FFAA35` | **10,0:1** | AAA — **texto do botão primário é escuro** |
| `#0D1017` | `#FF7A00` | **7,3:1** | AAA — hover mantém texto escuro |
| `#F5F7FA` | `#FFAA35` | **1,8:1** | **reprovado** — nunca texto claro sobre laranja |
| `#1B2230` | `#F5F7FA` | **14,8:1** | AAA (texto escuro em superfície clara) |
| `#FFAA35` | `#FFFFFF` | **1,9:1** | **reprovado** — laranja não serve como texto/ícone/borda fina no tema claro |
| `#FF7A00` | `#FFFFFF` | **2,6:1** | **reprovado** para texto; abaixo de 3:1 até para componente |
| `#94A3B8` | `#FFFFFF` | **2,6:1** | **reprovado** — texto secundário do tema claro usa a cor do sistema |

Consequências diretas:

- **Botão primário:** preenchimento `#FFAA35`, texto e ícone `#0D1017`, nos dois temas.
- **Tema claro:** o laranja só aparece como preenchimento (botão, handle, badge com texto escuro) ou como anel de foco **com espessura ≥ 2 px sobre fundo claro aceitando o contraste baixo como o Fluent faz**; texto e ícones de acento no claro precisam de um tom escuro derivado — **pendente de decisão do branding** (lacuna registrada no [`README.md`](README.md)).
- **Texto secundário no claro:** `TextFillColorSecondaryBrush` do Fluent, não `#94A3B8`.

## Semântica

Sem cores próprias de estado — usa as do Fluent/WinUI, que já cumprem contraste nos dois temas:

| Estado | Recurso WinUI | Onde |
|--------|---------------|------|
| Sucesso | `InfoBarSeverity.Success` · `SystemFillColorSuccess` | "Captura salva" na linha de status |
| Erro | `InfoBarSeverity.Error` · `SystemFillColorCritical` | Falha de captura/gravação |
| Aviso | `InfoBarSeverity.Warning` · `SystemFillColorCaution` | Conflito de atalho |
| Informativo | `InfoBarSeverity.Informational` | Configurações redefinidas |

O laranja da marca **não** é cor de aviso.

## Tokens (convenção para o `ResourceDictionary`)

```xml
<ResourceDictionary.ThemeDictionaries>
  <ResourceDictionary x:Key="Dark">
    <Color x:Key="SharpShotBackgroundColor">#0D1017</Color>
    <Color x:Key="SharpShotSurfaceColor">#1B2230</Color>
    <Color x:Key="SharpShotPrimaryColor">#FFAA35</Color>
    <Color x:Key="SharpShotPrimaryHoverColor">#FF7A00</Color>
    <Color x:Key="SharpShotTextColor">#F5F7FA</Color>
    <Color x:Key="SharpShotTextSecondaryColor">#94A3B8</Color>
    <Color x:Key="SharpShotOnPrimaryColor">#0D1017</Color>
  </ResourceDictionary>
  <ResourceDictionary x:Key="Light"><!-- ver temas.md --></ResourceDictionary>
  <ResourceDictionary x:Key="HighContrast"><!-- cores do sistema --></ResourceDictionary>
</ResourceDictionary.ThemeDictionaries>
```

`SolidColorBrush` correspondentes com sufixo `Brush`. Nenhum componente usa `Color` direto.

## Máscara do overlay

Fora da seleção: `#0D1017` com **60 %** de opacidade (`#990D1017`). Dentro: sem máscara (frame original). Valor a confirmar visualmente em monitores claros e escuros — **pendente de validação**.

## Nunca

- Hex ou `Color.FromArgb` no XAML/code-behind de componente.
- Laranja como fundo de página, de card ou de barra de título.
- Texto claro sobre laranja.
- `#94A3B8` como texto no tema claro.
- Gradiente na UI (o único gradiente é a prata do logo, gerada pelo script).

---

Temas: [`temas.md`](temas.md) · Estados: [`estados.md`](estados.md) · Guia rápido: [`../DESIGN.md`](../DESIGN.md)
