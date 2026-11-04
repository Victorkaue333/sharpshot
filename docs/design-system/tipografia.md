# Tipografia — Design system

> Uma família, a escala do Fluent, nenhuma fonte embutida.
> Fontes: WinUI 3 (`TextBlock` styles) · [`../DESIGN.md`](../DESIGN.md) · App (`src/SharpShot.App/Resources/Colors.xaml`, `Styles.xaml`, `Views/`).
> Última atualização: 04/nov/2026 · Mantenedor: Victor Alves

## Família

| Uso | Fonte | Fallback | Observação |
|-----|-------|----------|------------|
| Toda a interface | **Segoe UI Variable** (texto: *Segoe UI Variable Text*; títulos ≥ 20 px: *Segoe UI Variable Display*) | Segoe UI (Windows 10) | É a fonte padrão do WinUI 3 — não precisa declarar `FontFamily`; declarar só quebra o fallback |
| Atalhos exibidos (`Ctrl + Shift + 1`) | Segoe UI Variable, peso Semibold, em `CaptionTextBlockStyle` | — | Opcional: `Cascadia Mono`/`Consolas` para o editor de atalho — **decisão pendente**; a regra é não embutir fonte |
| Badge de tamanho do overlay (`1280 × 720`) | Segoe UI Variable Semibold 14 px com **numerais tabulares** | — | `FontFeatures` `tnum` (ou `Typography.NumeralAlignment="Tabular"`) para o texto não "dançar" ao arrastar; o `×` é U+00D7, não a letra x |
| Ícones | Segoe Fluent Icons (`FontIcon`) | Segoe MDL2 Assets (Win10) | Ver [`icones.md`](icones.md) |
| Logo | **não é texto** — é `PathGeometry` de `LogoGeometry.cs` | — | O wordmark "SharpShot" ao lado do símbolo na barra de título é `TextBlock` em `CaptionTextBlockStyle` Semibold |

Nenhuma fonte é distribuída com o app; nada de `ms-appx:///Assets/fonts/`.

## Escala (estilos do WinUI, não tamanhos à mão)

| Estilo WinUI | Tamanho / peso | Onde no SharpShot |
|--------------|----------------|-------------------|
| `TitleLargeTextBlockStyle` | 40 px / Semibold | — (não usado; título grande demais para a janela) |
| `TitleTextBlockStyle` | 28 px / Semibold | Título da Home ("Capturar") — opcional; a Home pode não ter título além dos cards |
| `SubtitleTextBlockStyle` | 20 px / Semibold | Cabeçalho de seção em Configurações (Geral · Atalhos · Salvamento · Aparência); nome do app em Sobre |
| `BodyStrongTextBlockStyle` | 14 px / Semibold | Título dos cards da Home ("Capturar tela inteira"); rótulo de cada configuração |
| `BodyTextBlockStyle` | 14 px / Regular | Corpo, descrições, texto do `InfoBar`, linha de status |
| `CaptionTextBlockStyle` | 12 px / Regular | Atalho no card, nome do arquivo na linha de status, texto auxiliar de configuração, convenção de nomes |
| `BodyLargeTextBlockStyle` | 18 px / Regular | Versão em Sobre |

Regras:

- Hierarquia por **peso e tamanho do Fluent**, não por cor. Cor secundária (`SharpShotTextSecondaryBrush`) só para informação auxiliar, nunca para "deixar mais bonito".
- Um degrau por nível: página → seção (`Subtitle`) → item (`BodyStrong`) → corpo/legenda. Sem pular do `Title` para o `Caption`.
- Sem `FontSize` numérico no XAML de componente; se um tamanho não existe na escala, o problema é o layout.

## Comprimento e quebra

- Rótulos de configuração em uma linha; descrição auxiliar em `Caption`, até duas linhas, `TextWrapping="Wrap"`.
- Caminho da pasta na linha "Salvar em": `TextTrimming="CharacterEllipsis"` **no início** não existe no WinUI — usar trimming normal e tooltip com o caminho completo; alternativa: mostrar só a pasta final com tooltip.
- Nome do arquivo no toast: o Windows trunca; o nome `SharpShot_2026-11-04_14-32-07-583.png` (37 caracteres) cabe em uma linha do toast padrão.
- Mensagens de erro ([`../architecture/erros.md`](../architecture/erros.md)): até ~2 linhas no `InfoBar`; o detalhe longo vai para o log.

## Idioma e pontuação

- pt-BR, sem anglicismo quando há termo corrente: "pasta", "atalho", "captura", "notificação", "bandeja"; "PNG", "DPI", "HDR" ficam como siglas.
- Botões no infinitivo ("Capturar", "Cancelar", "Abrir pasta", "Restaurar padrões", "Procurar…"); toggles como afirmação ("Iniciar com o Windows"); títulos de seção como substantivo ("Atalhos").
- Reticências em ações que abrem diálogo ("Procurar…"). Ponto final só em frases completas (mensagens de erro, descrições), não em rótulos.
- Espaço fino não é usado; "1280 × 720" usa espaços normais em volta do `×`.

## Acessibilidade

- Respeitar a escala de texto do Windows (*Acessibilidade → Tamanho do texto*): estilos do Fluent escalam; tamanhos fixos não.
- Contraste conforme [`cores.md`](cores.md).
- `AutomationProperties.Name` quando o texto visível não descreve o controle (botões só com ícone).

---

Cores: [`cores.md`](cores.md) · Espaçamento: [`espacamento.md`](espacamento.md) · Componentes: [`componentes.md`](componentes.md)
