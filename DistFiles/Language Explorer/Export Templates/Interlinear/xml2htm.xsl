<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
				version="1.0">
  <xsl:output method="html" version="4.0" encoding="UTF-8" omit-xml-declaration="yes" indent="yes" />
  <xsl:template match="document">
	<!-- NOTE, this DOCTYPE causes issues for tests that use AssertThatXmlIn to catch an error and display the DOM -->
	<!-- <xsl:text disable-output-escaping='yes'>&lt;!DOCTYPE html[]&gt;</xsl:text> -->
	<html>
	  <head>
		<style type="text/css">
		  number { vertical-align: top; }
<!--         span { display: inline-block; border: 1px solid black; vertical-align: top; } -->
		  span { display: -moz-inline-box; display: inline-block; vertical-align: top; }
		  table { text-align: left; }

		  .itx_Words { }
		  .itx_Frame_Number { }
		  .itx_Frame_Word { margin: 10px 5px 10px 5px; }
		  .itx_Homograph { font-size: xx-small; }
		  .itx_VariantTypes { font-variant: small-caps; }
		  .itx_txt { }
		  .itx_gls { }
		  .itx_pos { }
		  .itx_punct { }
		  .itx_morphemes { }
		  .itx_morph { }
		  .itx_morph_txt { }
		  .itx_morph_gls { }
		  .itx_morph_cf { }
		  .itx_morph_msa { }

		  .itx_Freeform { }
		  .itx_Freeform_gls { }
		  .itx_Freeform_lit { }
		  .itx_Freeform_note { }
		</style>
		<title> &#160; </title>
	  </head>
	  <body>
		<xsl:apply-templates/>
	  </body>
	</html>
  </xsl:template>

  <!-- INTERLINEAR-TEXT LEVEL -->

  <xsl:template match="interlinear-text">
	<xsl:apply-templates/>
  </xsl:template>

  <xsl:template match="interlinear-text/item">
	<xsl:apply-templates/>
  </xsl:template>

  <xsl:template match="interlinear-text/item[@type='title']">
	<h1>
		<xsl:attribute name="lang">
			<xsl:value-of select="@lang"/>
		</xsl:attribute>
		<xsl:apply-templates/>
  </h1>
  </xsl:template>
  <xsl:template match="interlinear-text/item[@type='title-abbreviation']"/>
  <xsl:template match="interlinear-text/item[@type='source']">
	<h2>
		<xsl:apply-templates/>
  </h2>
  </xsl:template>
  <xsl:template match="interlinear-text/item[@type='description']">
	<h2>
		<xsl:apply-templates/>
  </h2>
  </xsl:template>

  <!-- PARAGRAPH LEVEL -->

  <xsl:template match="paragraphs">
	<xsl:apply-templates/>
  </xsl:template>

  <xsl:template match="paragraph">
	<xsl:apply-templates/>
  </xsl:template>

  <!-- PHRASE LEVEL -->

  <xsl:template match="phrases">
	<xsl:apply-templates/>
  </xsl:template>

  <xsl:template match="phrase">
	<p class="itx_Words">
	  <xsl:apply-templates/>
	</p>
  </xsl:template>

  <xsl:template match="phrase/item">
	<xsl:choose>
	  <xsl:when test="@type='segnum'">
		<span class="itx_Frame_Number">
		  <xsl:attribute name="lang"><xsl:value-of select="@lang"/></xsl:attribute>
		  <xsl:value-of select="."/>
		</span>
	  </xsl:when>
	  <xsl:when test="@type='txt'">
		<xsl:apply-templates/>
		<br/>
	  </xsl:when>
	  <xsl:when test="@type='gls'">
		<div class="itx_Freeform_gls">
		  <xsl:apply-templates/>
		</div>
	  </xsl:when>
	  <xsl:when test="@type='lit'">
		<div class="itx_Freeform_lit">
		  <xsl:apply-templates/>
		</div>
	  </xsl:when>
	  <xsl:when test="@type='note'">
		<div class="itx_Freeform_note">
		  <xsl:apply-templates/>
		</div>
	  </xsl:when>
	</xsl:choose>
  </xsl:template>

  <!-- WORD LEVEL -->

  <xsl:template match="words">
	<xsl:apply-templates/>
  </xsl:template>

  <xsl:template match="word">
	<span class="itx_Frame_Word">
	  <table cellpadding="0" cellspacing="0">
		<xsl:apply-templates/>
	  </table>
	</span>
  </xsl:template>

  <xsl:template match="word/item[@type='txt']">
	<tr>
	  <td class="itx_txt">
		<xsl:apply-templates/>
		<xsl:text>&#160;</xsl:text>
	  </td>
	</tr>
  </xsl:template>

  <xsl:template match="word/item[@type='gls']">
	<tr>
	  <td class="itx_gls">
		<xsl:if test="string(.)">
		  <xsl:apply-templates/>
		  <xsl:text>&#160;</xsl:text>
		</xsl:if>
		<br/>
	  </td>
	</tr>
  </xsl:template>

  <xsl:template match="word/item[@type='pos']">
	<tr>
	  <td class="itx_pos">
		<xsl:if test="string(.)">
		  <xsl:apply-templates/>
		  <xsl:text>&#160;</xsl:text>
		</xsl:if>
		<br/>
	  </td>
	</tr>
  </xsl:template>

  <xsl:template match="word/item[@type='punct']">
	<tr>
	  <td class="itx_punct">
		<xsl:apply-templates/>
		<xsl:text>&#160;</xsl:text>
	  </td>
	</tr>
  </xsl:template>

  <!-- MORPHEME LEVEL -->

  <xsl:template match="morphemes">
	<tr>
	  <td class="itx_morphemes">
		<xsl:apply-templates/>
	  </td>
	</tr>
  </xsl:template>

  <xsl:template match="morph">
	<span class="itx_morph">
	  <table cellpadding="0" cellspacing="0">
		<xsl:apply-templates/>
	  </table>
	</span>
  </xsl:template>

  <xsl:template match="morph/item">
	<tr>
	  <td>
		<xsl:attribute name="class">
			<xsl:text>itx_</xsl:text>
			<xsl:value-of select="local-name(parent::*)"/>
			<xsl:text>_</xsl:text>
			<xsl:value-of select="@type"/>
		</xsl:attribute>
		<xsl:apply-templates/>
		<xsl:text>&#160;</xsl:text>
	  </td>
	</tr>
  </xsl:template>

  <!-- suppress homograph numbers and variant types, so they don't occupy an extra line-->
  <xsl:template match="morph/item[@type='hn']">
  </xsl:template>
  <xsl:template match="morph/item[@type='glsPrepend']">
  </xsl:template>
  <xsl:template match="morph/item[@type='variantTypes']">
  </xsl:template>
  <xsl:template match="morph/item[@type='glsAppend']">
  </xsl:template>
  <xsl:template match="languages">
  </xsl:template>

  <!-- This mode occurs within the 'cf' item to display the homograph number from the following item.-->
  <xsl:template match="morph/item[@type='hn']" mode="hn">
	<xsl:apply-templates/>
  </xsl:template>
  <xsl:template match="morph/item[@type='glsPrepend']" mode="glsPrepend">
	<span class="itx_VariantTypes"><xsl:apply-templates/></span>
  </xsl:template>
  <xsl:template match="morph/item[@type='variantTypes']" mode="variantTypes">
	<span class="itx_VariantTypes"><xsl:apply-templates/></span>
  </xsl:template>
  <xsl:template match="morph/item[@type='glsAppend']" mode="glsAppend">
	<span class="itx_VariantTypes"><xsl:apply-templates/></span>
  </xsl:template>


  <xsl:template match="morph/item[@type='cf']">
	<tr>
	  <td>
		<xsl:attribute name="class">
			<xsl:text>itx_</xsl:text>
			<xsl:value-of select="local-name(parent::*)"/>
			<xsl:text>_</xsl:text>
			<xsl:value-of select="@type"/>
		</xsl:attribute>
		<xsl:apply-templates/>
	   <xsl:variable name="homographNumber" select="following-sibling::item[1][@type='hn']"/>
		<xsl:if test="$homographNumber">
				<sub class="itx_Homograph"><xsl:apply-templates select="$homographNumber" mode="hn"/></sub>
		</xsl:if>
		<xsl:variable name="variantTypes" select="following-sibling::item[(count($homographNumber)+1)][@type='variantTypes']"/>
		<xsl:if test="$variantTypes">
		  <xsl:apply-templates select="$variantTypes" mode="variantTypes"/>
		</xsl:if>
		<xsl:text>&#160;</xsl:text>
	  </td>
	</tr>
  </xsl:template>

  <xsl:template match="morph/item[@type='gls']">
	<tr>
	  <td>
		<xsl:attribute name="class">
			<xsl:text>itx_</xsl:text>
			<xsl:value-of select="local-name(parent::*)"/>
			<xsl:text>_</xsl:text>
			<xsl:value-of select="@type"/>
		</xsl:attribute>
		<xsl:variable name="glsPrepend" select="preceding-sibling::item[1][@type='glsPrepend']"/>
		<xsl:if test="$glsPrepend">
		  <xsl:apply-templates select="$glsPrepend" mode="glsPrepend"/>
		</xsl:if>
		<xsl:apply-templates/>
		<xsl:variable name="glsAppend" select="following-sibling::item[1][@type='glsAppend']"/>
		<xsl:if test="$glsAppend">
		  <xsl:apply-templates select="$glsAppend" mode="glsAppend"/>
		</xsl:if>
		<xsl:text>&#160;</xsl:text>
	  </td>
	</tr>
  </xsl:template>

  <!-- MISCELLANEOUS -->

  <xsl:template match="i">
	<i>
	  <xsl:apply-templates/>
	</i>
  </xsl:template>

  <xsl:template match="b">
	<b>
	  <xsl:apply-templates/>
	</b>
  </xsl:template>

  <xsl:template match="title">
	<block font-style="bold">
	  <xsl:apply-templates/>
	</block>
  </xsl:template>


  <xsl:template match="*">
	<xsl:copy>
	  <xsl:copy-of select="@*"/>
	  <xsl:apply-templates/>
	</xsl:copy>
  </xsl:template>

</xsl:stylesheet>
