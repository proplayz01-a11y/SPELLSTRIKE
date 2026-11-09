from pathlib import Path

from docx import Document
from docx.enum.section import WD_SECTION
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from docx.shared import Inches, Pt, RGBColor


OUTPUT = Path(r"C:\Users\David\PROTOTYPE\SPELLSTRIKE\Deliverables\Angelito_Tadeo_Portfolio_Cover_Letter.docx")


def set_cell_margins(cell, top=0, start=0, bottom=0, end=0):
    tc = cell._tc
    tc_pr = tc.get_or_add_tcPr()
    tc_mar = tc_pr.first_child_found_in("w:tcMar")
    if tc_mar is None:
        tc_mar = OxmlElement("w:tcMar")
        tc_pr.append(tc_mar)
    for margin, value in (("top", top), ("start", start), ("bottom", bottom), ("end", end)):
        node = tc_mar.find(qn(f"w:{margin}"))
        if node is None:
            node = OxmlElement(f"w:{margin}")
            tc_mar.append(node)
        node.set(qn("w:w"), str(value))
        node.set(qn("w:type"), "dxa")


def set_font(run, name="Aptos", size=11, bold=False, color="222222"):
    run.font.name = name
    run._element.get_or_add_rPr().rFonts.set(qn("w:ascii"), name)
    run._element.get_or_add_rPr().rFonts.set(qn("w:hAnsi"), name)
    run.font.size = Pt(size)
    run.font.bold = bold
    run.font.color.rgb = RGBColor.from_string(color)


def set_spacing(paragraph, before=0, after=7, line=1.16):
    fmt = paragraph.paragraph_format
    fmt.space_before = Pt(before)
    fmt.space_after = Pt(after)
    fmt.line_spacing = line


def add_body(doc, text, before=0, after=8):
    p = doc.add_paragraph()
    set_spacing(p, before=before, after=after)
    set_font(p.add_run(text), size=11, color="222222")
    return p


OUTPUT.parent.mkdir(parents=True, exist_ok=True)
doc = Document()
section = doc.sections[0]
section.page_width = Inches(8.5)
section.page_height = Inches(11)
section.top_margin = Inches(0.7)
section.bottom_margin = Inches(0.65)
section.left_margin = Inches(0.82)
section.right_margin = Inches(0.82)

styles = doc.styles
normal = styles["Normal"]
normal.font.name = "Aptos"
normal._element.rPr.rFonts.set(qn("w:ascii"), "Aptos")
normal._element.rPr.rFonts.set(qn("w:hAnsi"), "Aptos")
normal.font.size = Pt(11)

title_style = styles["Title"]
title_style.font.name = "Aptos Display"
title_style._element.rPr.rFonts.set(qn("w:ascii"), "Aptos Display")
title_style._element.rPr.rFonts.set(qn("w:hAnsi"), "Aptos Display")
title_style.font.size = Pt(14)
title_style.font.bold = True
title_style.font.color.rgb = RGBColor(0, 0, 0)
title_style_ppr = title_style.element.get_or_add_pPr()
for border in title_style_ppr.xpath("./w:pBdr"):
    title_style_ppr.remove(border)

title = doc.add_paragraph(style="Title")
title.alignment = WD_ALIGN_PARAGRAPH.LEFT
title.paragraph_format.space_after = Pt(14)
set_font(title.add_run("Portfolio Client Cover Letter"), name="Aptos Display", size=14, bold=True, color="000000")
title_ppr = title._p.get_or_add_pPr()
for border in title_ppr.xpath("./w:pBdr"):
    title_ppr.remove(border)

header = doc.add_table(rows=1, cols=2)
header.autofit = False
header.columns[0].width = Inches(3.9)
header.columns[1].width = Inches(2.8)
left, right = header.rows[0].cells
set_cell_margins(left, 0, 0, 0, 0)
set_cell_margins(right, 0, 0, 0, 0)

p = left.paragraphs[0]
set_spacing(p, after=1)
set_font(p.add_run("ANGELITO TADEO"), name="Aptos Display", size=20, bold=True, color="111111")
p = left.add_paragraph()
set_spacing(p, after=0)
set_font(p.add_run("Email Designer  |  Creative Designer"), size=10.5, bold=True, color="B45335")

contact = ["[Email Address]", "[Phone or WhatsApp]", "[Portfolio Link]", "[LinkedIn Profile]"]
for i, item in enumerate(contact):
    p = right.paragraphs[0] if i == 0 else right.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.RIGHT
    set_spacing(p, after=1)
    set_font(p.add_run(item), size=9.5, color="666666")

p = doc.add_paragraph()
set_spacing(p, before=16, after=3)
set_font(p.add_run("[Date]"), size=10, color="666666")
p = doc.add_paragraph()
set_spacing(p, after=1)
set_font(p.add_run("[Client Name]"), size=10.5, bold=True, color="222222")
p = doc.add_paragraph()
set_spacing(p, after=12)
set_font(p.add_run("[Company Name]"), size=10.5, color="555555")

p = doc.add_paragraph()
set_spacing(p, after=10)
set_font(p.add_run("Subject  "), size=11, bold=True, color="111111")
set_font(p.add_run("Premium Email Design That Strengthens Your Brand"), size=11, bold=True, color="B45335")

add_body(doc, "Dear [Client Name],")
add_body(doc, "I am Angelito Tadeo, a creative designer focused on polished email campaigns and branded digital content. I help businesses turn their ideas into clear, engaging visual experiences that support their brand and marketing goals.")
add_body(doc, "My work combines strong visual hierarchy, thoughtful typography, compelling imagery, and clear calls to action. My portfolio includes concept campaigns for performance, lifestyle, food, and beverage brands, demonstrating how I adapt each design to its audience while maintaining a refined and consistent presentation.")

p = doc.add_paragraph()
set_spacing(p, before=2, after=5)
set_font(p.add_run("For your project, I can provide:"), size=11, bold=True, color="111111")
services = [
    "Custom email campaign designs",
    "Promotional and product launch creatives",
    "Mobile friendly layouts",
    "Brand consistent typography and colors",
    "Clear revision and delivery timelines",
    "Professional communication throughout the project",
]
for service in services:
    p = doc.add_paragraph(style="List Bullet")
    p.paragraph_format.left_indent = Inches(0.25)
    p.paragraph_format.first_line_indent = Inches(-0.15)
    set_spacing(p, after=2, line=1.05)
    set_font(p.add_run(service), size=10.5, color="333333")

add_body(doc, "You can view my portfolio here: [Portfolio Link]", before=5)
add_body(doc, "I would be glad to learn more about your brand, audience, and campaign goals. If my work matches the quality you are looking for, I would welcome the opportunity to discuss how we can create something valuable together.")
add_body(doc, "Thank you for your time and consideration.", after=10)

p = doc.add_paragraph()
set_spacing(p, after=2)
set_font(p.add_run("Warm regards,"), size=11, color="222222")
p = doc.add_paragraph()
set_spacing(p, after=1)
set_font(p.add_run("Angelito Tadeo"), name="Aptos Display", size=12.5, bold=True, color="111111")
p = doc.add_paragraph()
set_spacing(p, after=0)
set_font(p.add_run("Email Designer  |  Creative Designer"), size=10, color="666666")

core = doc.core_properties
core.title = "Portfolio Client Cover Letter"
core.subject = "Professional introduction for prospective design clients"
core.author = "Angelito Tadeo"
core.keywords = "portfolio, cover letter, email design, creative design"

doc.save(OUTPUT)
print(OUTPUT)
