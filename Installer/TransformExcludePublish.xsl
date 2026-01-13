<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0"
                xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:wix="http://wixtoolset.org/schemas/v4/wxs">

  <xsl:output method="xml" indent="yes"/>

  <!-- Identity template - copy everything by default -->
  <xsl:template match="@*|node()">
    <xsl:copy>
      <xsl:apply-templates select="@*|node()"/>
    </xsl:copy>
  </xsl:template>

  <!-- Exclude the entire 'publish' Directory element and all its children -->
  <xsl:template match="wix:Directory[@Name='publish']"/>

  <!-- Exclude any DirectoryRef that has 'publish' in its Id -->
  <xsl:template match="wix:DirectoryRef[contains(@Id, 'publish')]"/>

  <!-- Exclude any Component that has 'publish' in its Directory attribute -->
  <xsl:template match="wix:Component[contains(@Directory, 'publish')]"/>

  <!-- Exclude any File that has '\publish\' in its Source path -->
  <xsl:template match="wix:Component[contains(wix:File/@Source, '\publish\')]"/>

  <!-- Exclude any ComponentRef that has 'publish' in its Id -->
  <xsl:template match="wix:ComponentRef[contains(@Id, 'publish')]"/>

</xsl:stylesheet>