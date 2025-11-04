<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0"
                xmlns:xsl="http://www.w3.org/1999/XSL/Transform">

  <xsl:output method="xml" indent="yes" encoding="UTF-8"/>

  <!-- 
       Основной шаблон: создаём корневой элемент <Employees>
       и группируем все <item> по комбинации name + surname
  -->
  <xsl:template match="/Pay">
    <Employees>
      <!-- Выбираем все <item> из двух возможных структур -->
      <xsl:variable name="all-items"
                    select="item | january/item | february/item | march/item"/>

      <!-- Группируем по уникальному сотруднику (name + surname) -->
      <xsl:for-each select="$all-items[
                              generate-id() = 
                              generate-id(key('by-employee', 
                                              concat(@name, '|', @surname))[1])
                            ]">
        <xsl:sort select="@name"/>
        <xsl:sort select="@surname"/>

        <xsl:variable name="emp-key" select="concat(@name, '|', @surname)"/>
        <Employee name="{@name}" surname="{@surname}">
          <!-- Сортируем зарплаты по месяцам: january → february → march -->
          <xsl:for-each select="key('by-employee', $emp-key)">
            <xsl:sort select="concat(
                               substring('january  ', 1, 7 - string-length(@mount)),
                               @mount)"/>
            <salary amount="{@amount}" mount="{@mount}"/>
          </xsl:for-each>
        </Employee>
      </xsl:for-each>
    </Employees>
  </xsl:template>

  <!-- Ключ для группировки по сотруднику -->
  <xsl:key name="by-employee" 
           match="item" 
           use="concat(@name, '|', @surname)"/>

</xsl:stylesheet>