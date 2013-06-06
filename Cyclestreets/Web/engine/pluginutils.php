<?php
/*********************************************************
 * $HeadURL: http://www.maidenfans.com/svn/rwdownload_new/TRUNK/engine/pluginutils.php $
 * $Revision: 50 $
 * $LastChangedBy: realworld $
 * $LastChangedDate: 2008-12-20 20:32:52 +0000 (Sat, 20 Dec 2008) $
 *********************************************************/
 
class pluginutils
{
    var $output;
    
    function pluginutils()
    {
        global $IN, $OUTPUT, $clang;
        
        switch ($IN['ACT'])
        {
            case 'createplugin':
                $this->installPluginFromBlank();
                break;
        }
        
        $OUTPUT->add_output($this->output);
    }
    
    function installPluginFromBlank()
    {
        global $modloader, $guser, $std, $DB, $IN;
        
        $name = $std->makeAlphaNumeric($IN['name']);
        $this->parseModuleData($name);
        if ( !$modloader->installed[$name] )
        {
            $insert = array("modname" => $name,
                            "author" => $guser->username,
                            "enabled" => 1);
            $DB->insert($insert, "dl_plugins");
            $std->info(GETLANG("plugin_installed")); 
        }          
    }
    
    function parseModuleData($name)
    {
        require_once (ROOT_PATH."/engine/xml.php");
        
        $xml = 0;
        if ( is_file(ROOT_PATH."/modules/{$name}/module_info.xml") )
        {   
            $xml = new CXMLUtils();
            $xml->parseFile(ROOT_PATH."/modules/{$name}/module_info.xml");
        }
        
        $sqldata = $xml->GetChildElementsFromTag("sql");
        if ( !empty($sqldata['children']) )
        {
            foreach( $sqldata['children'] as $table )
            {
                //echo $table['Attributes']['name']."<br>";
                $columns = $xml->GetChildElementsFromTag("cols", $table['path']);
                $rows = $xml->GetChildElementsFromTag("rows", $table['path']);

                $this->installTables($table['Attributes']['name'], $columns['children'], $rows['children']);
            }
        }
    }
    
    function installTables($tableName, $collist=array(), $rowlist=array())
    {
        global $std, $DB;
        
        $query = "CREATE TABLE `{$tableName}` (";
        $insertquery = array();
        
        $first = 1;
        if ( !empty($collist) )
        {
            $primary = "";
            $unique = "";
            
            foreach ( $collist as $column )
            {
                $col = $column['Attributes'];
                
                if ( !$first )
                    $query = rtrim($query).", \n";
                    
                $query .= "`{$col['field']}` {$col['type']}";
                $query .= ( $col['length'] ) ? "({$col['length']}) " : " ";
                $query .= "NOT NULL ";
                if ( $col['default'] ) $query .= "default '{$col['default']}' ";
                if ( $col['auto_increment'] ) $query .= "auto_increment ";
                if ( $col['primary'] ) $primary = $col['field'];
                if ( $col['unique'] ) $unique = $col['field'];
                
                $first = 0;
            }
            if ( !empty($rowlist) )
            {
                foreach ( $rowlist as $row )
                {
                    $insertquery[] = $DB->createInsertQuery($row['Attributes'], $tableName)."; ";
                }
            }       
        }
        else
        {
            $std->error("er_tableNoCols");
            return;
        }
        
        if ( $primary ) $query .= ", PRIMARY KEY (`{$primary}`)";
        if ( $unique ) $query .= ", UNIQUE KEY `{$unique}` (`{$unique}`)";

        $query .= ")";
        
        $DB->query($query);
        if ( !empty($insertquery) )
        {
            foreach( $insertquery as $insert )
                $DB->query($insert);
        }
    }
}

$loader = new pluginutils();

?>
