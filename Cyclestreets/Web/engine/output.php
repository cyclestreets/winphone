<?php
/*********************************************************
 * $HeadURL: http://www.maidenfans.com/svn/rwdownload_new/TRUNK/engine/output.php $
 * $Revision: 50 $
 * $LastChangedBy: realworld $
 * $LastChangedDate: 2008-12-20 20:32:52 +0000 (Sat, 20 Dec 2008) $
 *********************************************************/

class CDisplay
{
	var $output;
	var $debug;
	
	function CDisplay()
	{
		global $rwdInfo, $CONFIG;
		// Open and save the wrapper
		$filename = $rwdInfo->skinpath."/main.htm";
		$handle = fopen($filename, "r");
		$rwdInfo->skin_wrapper = fread($handle, filesize($filename));
		fclose($handle);
        
        // Do something very important
        if ( ENCODED and sg_get_const("nocopy") == "true" )
        {
            if ( !INSTALL )
            {
                if ( !($fout=fopen($rwdInfo->path."/skins/skin".$CONFIG["defaultSkin"]."/main.htm","r")) )
                    die("Can not write to {$rwdInfo->path}/skins/skin{$CONFIG['defaultSkin']}/main.htm");
                $template = fread($fout, filesize($rwdInfo->path."/skins/skin".$CONFIG["defaultSkin"]."/main.htm"));
                if (!stristr($template, '{copyright}'))
                {
                    die(GETLANG("warn_copyright"));
                }
                fclose($fout);
            }
        }
	}
	
	// =====================================
	// Loads new style template data
	// =====================================
	function load_template( $name )
	{
		global $CONFIG, $rwdInfo;
					
		if ( $name != 'skin_global')
		{
			if ( ! in_array( 'skin_global', $rwdInfo->loaded_templates ) )
			{
				require_once( $rwdInfo->skinpath."/skin_global.php" );
				$rwdInfo->skin_global        = new skin_global();
				$rwdInfo->loaded_templates[] = 'skin_global';
			}
			
			$rwdInfo->loaded_templates[] = $name;

			require_once( $rwdInfo->skinpath."/".$name.".php" );

			return new $name();
			
		}
		else
		{
			$rwdInfo->loaded_templates[] = 'skin_global';

			require_once( $rwdInfo->skinpath."/skin_global.php" );

			$rwdInfo->skin_global = new skin_global();
			return;
		}
	}

	function add_output($to_add)
    {
        $this->output .= $to_add;
    }
	
	function print_output()
	{
		global $CONFIG, $DB, $std, $guser, $module, $version, $rwdInfo;

        if ( ENCODED and sg_get_const("nocopy") == "true" )
        {
            $copyright = "";
        }
		else
        {
            if ( !$std->isUsingFullVersion() )
            {
                $version .= " lite";
            }
            $copyright = "<!-- Copyright Information -->\n\n<span class='copyright' style='font-size: 10px'>Powered by <a href='http://www.rwscripts.com/' target='_blank'>RW::Download</a> $version<br>© 2008 <a href='http://www.rwscripts.com/' target='_blank'>RW::Scripts</a></span>\n\n";
		}

        $DB->query("SELECT direction, chrset FROM dl_langsets WHERE `lid`='".$std->getCurrentLanguage()."'");
        $langrow = $DB->fetch_row();
        
		$this->showDebug();

		//$copyright = $this->debug.$copyright;
        $rwdInfo->skin_wrapper = $module->html_globalskincall($rwdInfo->skin_wrapper);
        $rwdInfo->skin_wrapper = str_replace( "{textdir}" , $langrow['direction'], $rwdInfo->skin_wrapper);
        $rwdInfo->skin_wrapper = str_replace( "{charset}" , $langrow['chrset'], $rwdInfo->skin_wrapper);
        if (stristr($rwdInfo->skin_wrapper, "{links}"))
		    $rwdInfo->skin_wrapper = str_replace( "{links}" , $rwdInfo->links, $rwdInfo->skin_wrapper);
        if (stristr($rwdInfo->skin_wrapper, "{userbar}"))
		    $rwdInfo->skin_wrapper = str_replace( "{userbar}" , $rwdInfo->userbar, $rwdInfo->skin_wrapper);
        if (stristr($rwdInfo->skin_wrapper, "{nav}"))
		    $rwdInfo->skin_wrapper = str_replace( "{nav}" , $rwdInfo->nav, $rwdInfo->skin_wrapper);
        $rwdInfo->skin_wrapper = str_replace( "{copyright}" , $copyright, $rwdInfo->skin_wrapper);
        if (stristr($rwdInfo->skin_wrapper, "{style}"))
		    $rwdInfo->skin_wrapper = str_replace( "{style}" , "<LINK href='{skin_path}/style.css' type='text/css' rel='stylesheet'>", $rwdInfo->skin_wrapper);
        if (stristr($rwdInfo->skin_wrapper, "{skin_path}"))
		    $rwdInfo->skin_wrapper = str_replace( "{skin_path}" , $rwdInfo->skinurl, $rwdInfo->skin_wrapper);
        if (stristr($rwdInfo->skin_wrapper, "{script_path}"))
		    $rwdInfo->skin_wrapper = str_replace( "{script_path}" , $rwdInfo->url, $rwdInfo->skin_wrapper);
        if (stristr($rwdInfo->skin_wrapper, "{userdb_js}"))
            $rwdInfo->skin_wrapper = str_replace( "{userdb_js}" , $guser->userdb_js(), $rwdInfo->skin_wrapper);
        if (stristr($rwdInfo->skin_wrapper, "{main_title}"))
        {
            $title = $CONFIG['sitename'];
            if ( $rwdInfo->currentPageName )
                $title .= " > ".$rwdInfo->currentPageName;
            if ($CONFIG['isoffline'])
                $title .= " [OFFLINE]";
		    $rwdInfo->skin_wrapper = str_replace( "{main_title}" , $title, $rwdInfo->skin_wrapper);
        }
		// Stats tests
		if ( stristr($rwdInfo->skin_wrapper, "{top_downloads,") ||
             stristr($rwdInfo->skin_wrapper, "{top_rated,") ||
             stristr($rwdInfo->skin_wrapper, "{new_downloads,") ||
             stristr($rwdInfo->skin_wrapper, "{file_stats}") ||
             stristr($rwdInfo->skin_wrapper, "{random_download,") )
		{
			include_once ROOT_PATH."/functions/stats.php";
			$stats = new stats();
            $limit = $CONFIG['num_stats'];
            if ( stristr($rwdInfo->skin_wrapper, "{top_downloads,") )
			    $rwdInfo->skin_wrapper = preg_replace( "#(?:\s+?)?{top_downloads,(.+?)}#ise", "\$stats->topDownloads('\\1')", $rwdInfo->skin_wrapper );
            if ( stristr($rwdInfo->skin_wrapper, "{top_rated,") )
			    $rwdInfo->skin_wrapper = preg_replace( "#(?:\s+?)?{top_rated,(.+?)}#ise", "\$stats->topRatedDownloads('\\1')", $rwdInfo->skin_wrapper );
            if ( stristr($rwdInfo->skin_wrapper, "{new_downloads,") )
			    $rwdInfo->skin_wrapper = preg_replace( "#(?:\s+?)?{new_downloads,(.+?)}#ise", "\$stats->latestDownloads('\\1')", $rwdInfo->skin_wrapper );
            if ( stristr($rwdInfo->skin_wrapper, "{file_stats}") )
			    $rwdInfo->skin_wrapper = str_replace( "{file_stats}" , $stats->totalDownloads(), $rwdInfo->skin_wrapper);
            if ( stristr($rwdInfo->skin_wrapper, "{random_download,") )
			    $rwdInfo->skin_wrapper = preg_replace( "#(?:\s+?)?{random_download,(.+?),(.+?)}#ise", "\$stats->showRandomDownload('\\2', '\\1')", $rwdInfo->skin_wrapper );
		}
		if ( $rwdInfo->error_log )
			$rwdInfo->skin_wrapper = str_replace( "{main_content}" , $rwdInfo->error_log.$this->debug, $rwdInfo->skin_wrapper);
		else
			$rwdInfo->skin_wrapper = str_replace( "{main_content}" , $this->output.$this->debug, $rwdInfo->skin_wrapper);

		// Enable GZip?
		if ( $CONFIG['usegzip'] )
		{
			if(extension_loaded("zlib")) 
			{
				ob_start("ob_gzhandler");
			}
		}
		print $rwdInfo->skin_wrapper;
	}
	
	function rwWordWrap($text)
	{
		global $CONFIG;
		$maxLen = $CONFIG['max_word_length'];   // @todo Make this configurable
		$strings = explode(" ", $text);
		foreach ( $strings as $word )
		{
			if ( strlen( $word ) > $maxLen )
			{
				$begin = substr($word, 0, $maxLen);
				$end = substr($word, $maxLen, strlen($word) - $maxLen);
				$new = $begin . " " . $this->rwWordWrap($end);
				$final .= $new." ";
			}
			else
				$final .= $word." ";
		}
		return $final;
	}
	
	function showDebug()
    {
    	global $CONFIG, $IN, $DB, $std, $guser, $rwdInfo;
    	      
       //+----------------------------------------------
       // $IN values
       //+----------------------------------------------

       if ( !$guser->isAdmin )
        return;
        
	   if ($CONFIG['debuglevel'])
	   		$this->debug = "<div class='debugborder'>";
       if ($CONFIG['debuglevel'] >= 3)
       {
       		$output = "<hr><b>POST &amp; GET:</b><br>";
        	
			$output .= $this->new_table();
			foreach($IN as $k=>$v )
			{
				$output .= $this->new_row()."<b>$k</b> = ";
				if ( is_array($v) )
					$output .= $this->new_col().$this->echoArray($v);
				else
					$output .= $this->new_col().$std->makeSafe($v);
			}
			
			$output .= $this->end_table();
        	$this->debug .= $output;
        }
        
        //+----------------------------------------------
        // SQL
        //+----------------------------------------------
        
        if ($CONFIG['debuglevel'] >= 2)
        {
           	$output = "<hr><b>Queries:</b><br>";
			if ($CONFIG["usertype"] != USER_DEFAULT and $guser->userdb->query_string)
				$query_string = array_merge($DB->query_string, $guser->userdb->query_string);	
			else
				$query_string = $DB->query_string;
            if ( !empty($query_string) )
            {
                foreach($query_string as $q)
            	{
            		$q = htmlspecialchars($q);
					$boldwordsfind = array(
						"/(SELECT|UNION) /i",
						"/(UPDATE|INSERT) /i",
						"/DELETE /i",
						"/\s(FROM|SET|AS|WHERE|AND|OR|ORDER BY|GROUP BY|LIMIT|LEFT JOIN|RIGHT JOIN|NATURAL JOIN) /i",
						"/ (COUNT|IN|MIN|MAX|SUM|AVG|ON|VALUES)(\s)?\((.*?)\)/is",
						"/ INTO (.*?) \((.*?)\)/is"
					);
					$boldwordsreplacement = array(
						"<font color='#808000'><b>\${1}</b></font> ",
						"<font color='#FF8040'><b>\${1}</b></font> ",
						"<font color='#FF0000'><b>DELETE</b></font> ",
						" <b>\${1}</b> ",
						" <b>\${1}\${2}(</b>\${3}<b>)</b>",
						" <b>INTO</b> \${1} <b>(</b>\${2}<b>)</b> "
					);
					$q = preg_replace($boldwordsfind, $boldwordsreplacement, $q);
            		/*$q = preg_replace( "/^SELECT/i" , "<span class='red'>SELECT</span>"   , $q );
            		$q = preg_replace( "/^UPDATE/i" , "<span class='blue'>UPDATE</span>"  , $q );
            		$q = preg_replace( "/^DELETE/i" , "<span class='orange'>DELETE</span>", $q );
            		$q = preg_replace( "/^INSERT/i" , "<span class='green'>INSERT</span>" , $q );
            		$q = str_replace( "LEFT JOIN"   , "<span class='red'>LEFT JOIN</span>" , $q );*/

            		//$q = preg_replace( "/(".$ibforums->vars['sql_tbl_prefix'].")(\S+?)([\s\.,]|$)/", "<span class='purple'>\\1\\2</span>\\3", $q );

            		$output .= $q."<br>";
            	}
            }
			$this->debug .= $output;
        }
		
		if ( $CONFIG['debuglevel'] >= 1 )
		{
			$load = "";
			$output = "<hr><b>Debug Information:</b><br>";
            if ($CONFIG["usertype"] != USER_DEFAULT and $guser->userdb->query_string)
			    $total_queries = $DB->query_count + $guser->userdb->query_count;
            else
                $total_queries = $DB->query_count;
                
			if ( @file_exists('/proc/loadavg') )
			{
				if ( ($fh = @fopen( '/proc/loadavg', 'r' )) )
				{
					$data = @fread( $fh, 6 );
					@fclose( $fh );
			
					$load_avg = explode( " ", $data );
			
					$load = " <br><b>".trim($load_avg[0])."</b>";
				}
			}
			$output .= "SQL Queries: ".$total_queries;
			if ( $load )
				$output .= " Server Load: ".$load;
			$output .= "<br>";
			$this->debug .= $output;
		}
		if ($CONFIG['debuglevel'])
	   		$this->debug .= "</div>";
    }
	
	function echoArray($array)
	{
		$output = "<pre>";
		ob_start();
		print_r($array);
		$output .= ob_get_contents();
		ob_end_clean();
		$output .= "</pre>";
		return $output;
	}
	
	function tableSetup($colspan = -1, $class="", $tdclass="", $width="", $colwidth="")
	{
		$return = array();
		if ( $colspan != -1 )
			$return["colspan"] = " colspan='$colspan' ";
		if ( $class )
			$return["class"] = " class='$class' ";
		if ( $tdclass )
			$return["tdclass"] = " class='$tdclass' ";
		if ( $width )
			$return["width"] = " width='$width' ";
		if ( $colwidth )
			$return["colwidth"] = " width='$colwidth' ";
		return $return;
	}
	
	function new_table($colspan = -1, $class="", $tdclass="", $width="100%", $colwidth="", $padding=2)
	{
		$data = $this->tableSetup($colspan, $class, $tdclass, $width, $colwidth);
	
		$output = "<table summary='table'".$data['width']." border='0' cellspacing='1' cellpadding='$padding'>\n";
		$output .= "<tr".$data['class'].">\n";
		$output .= "<td valign='top'".$data['colspan'].$data['colwidth'].$data['tdclass'].">\n";
		return $output;
	}
	function new_row($colspan = -1, $class="", $tdclass="", $width="")
	{
		$data = $this->tableSetup($colspan, $class, $tdclass, "", $width);
		$output = "</td>\n</tr>\n<tr".$data["class"].">\n";
		$output .= "<td valign='top'".$data["colspan"].$data["colwidth"].$data["tdclass"].">\n";
		return $output;
	}
	function new_col($colspan = -1, $tdclass="", $width="")
	{
		$data = $this->tableSetup($colspan, "", $tdclass, $width);
		$output = "</td>\n";
		$output .= "<td valign='top'".$data["colspan"].$data["colwidth"].$data["tdclass"].">\n";
		return $output;
	}
	function end_table()
	{
		$output = "</td>\n</tr>\n</table>\n";
		return $output;
	}
}

?>