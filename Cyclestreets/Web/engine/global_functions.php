<?php
/*********************************************************
 * $HeadURL: http://www.maidenfans.com/svn/rwdownload_new/TRUNK/engine/global_functions.php $
 * $Revision: 50 $
 * $LastChangedBy: realworld $
 * $LastChangedDate: 2008-12-20 20:32:52 +0000 (Sat, 20 Dec 2008) $
 *********************************************************/

//define('DEMO', 0);

define('USER_DEFAULT', "rwd4");


define('LOGTYPE_ADMIN', 3);
define('LOGTYPE_ERROR', 4);

if (function_exists("set_time_limit"))
	@set_time_limit(0);

if (DEBUG)
	require_once ROOT_PATH."/engine/debug.php";

class func
{
    var $errorCount = 0;

	function func()
	{
		global $OUTPUT;
	}
    
	function convertDate($date)
	{
		global $CONFIG;
		$break = explode(" ", $date);
		$datebreak = explode("-", $break[0]);
		$time = explode(":", $break[1]);
		$epoch = date("U", mktime($time[0],$time[1],$time[2],$datebreak[1],$datebreak[2],$datebreak[0]));
		//$datetime = date("Y-m-d H:i:s", mktime($time[0],$time[1],$time[2],$datebreak[1],$datebreak[2],$datebreak[0]));
		$timeadjust = ($CONFIG['timeadjust'] * 60 * 60);
		return date($CONFIG["dateformat"],$epoch+$timeadjust);
	}

    function formatDate($time)
	{
		global $CONFIG;
		$timeadjust = ($CONFIG['timeadjust'] * 60 * 60);
		return date($CONFIG["dateformat"],$time+$timeadjust);
	}

	function converttotime($date)
	{
		if (!$date) 
		{
			if (DEBUG)
				echo "WARNING: No date passed to converttotime";
		    return;
		}
		$break = explode(" ", $date);
		$datebreak = explode("-", $break[0]);
		$time = explode(":", $break[1]);
		$epoch = date("U", mktime($time[0],$time[1],$time[2],$datebreak[1],$datebreak[2],$datebreak[0]));
		//$datetime = date("Y-m-d H:i:s", mktime($time[0],$time[1],$time[2],$datebreak[1],$datebreak[2],$datebreak[0]));
		return $epoch;
	}
	
	function isRecent($date) 
	{ 
		$break = explode(" ", $date); 
		$datebreak = explode("-", $break[0]); 
		$time = explode(":", $break[1]); 
		if ( $datebreak[0] == 0 )
			return false;
		$epoch = date("U", mktime($time[0],$time[1],$time[2],$datebreak[1],$datebreak[2],$datebreak[0]));
		$time = time();
		if ( $time - $epoch < 259200 )
			return true;
		else
			return false;
	}

	function mycopy($source, $dest)
	{
	    // Simple copy for a file
	    if (is_file($source)) {
	        return copy($source, $dest);
	    }

	    // Make destination directory
	    if (!is_dir($dest)) {
	        mkdir($dest);
	    }
	
	    // Loop through the folder
	    $dir = dir($source);
	    while (false !== $entry = $dir->read()) 
		{
	        // Skip pointers
	        if ($entry == '.' || $entry == '..') 
			{
	            continue;
	        }
	
	        // Deep copy directories
	        if (is_dir("$source/$entry") and ($dest !== "$source/$entry")) 
			{
	            $this->mycopy("$source/$entry", "$dest/$entry");
	        } 
			else 
			{
	            copy("$source/$entry", "$dest/$entry");
	        }
	    }
	
	    // Clean up
	    $dir->close();
	    return true;
	}
		
	function error($message)
	{
		global $OUTPUT, $rwdInfo;

        if ( DEBUG )
        {
            ob_start();
            echo "<pre>";
    		getTrace(1);
            echo "</pre>";
    		$stack = ob_get_contents();
    		ob_end_clean();
        }
        if ( INSTALL )
        {
            echo $message."<br>{$stack}<br>";
            return;
        }
        $message = $message.". ".GETLANG("er_stderror")."<br>{$stack}<br><a href=\"javascript: history.back()\">".GETLANG("back")."</a>";

		$data = array("message" => "$message");
		$OUTPUT->load_template("skin_global");
		$rwdInfo->error_log .= $rwdInfo->skin_global->error($data);
        $rwdInfo->nav = "";
        $this->addErrorLog($message);
		return "";
	}
	function warning($message)
	{
		global $OUTPUT,$rwdInfo;
		$data = array("message" => "$message");
		$OUTPUT->load_template("skin_global");
		$rwdInfo->error_log .= $rwdInfo->skin_global->warning($data);
        
		//$OUTPUT->add_output($info); 
	}
	// TODO: Phase this out
	function info($message)
	{
		global $OUTPUT,$rwdInfo;
		$data = array("message" => "$message");
		$OUTPUT->load_template("skin_global");
		$info = $rwdInfo->skin_global->info($data);
		$OUTPUT->add_output($info);
	}
	
	
	function GetFileExtention($filename)
	{ 
	    $ext = strchr($filename,"."); 
	    return $ext; 
	}

    function loadNewStyleConfig()
    {
        global $CONFIG, $DB;
        $DB->query("SELECT `key`, `value` FROM `dl_config`");
        while ( $myrow = $DB->fetch_row() )
        {
            $CONFIG[$myrow['key']] = $myrow['value'];
        }
    }

    function saveNewStyleConfig()
    {
        global $NEWCONFIG, $DB;

        // This won't work will it?
        foreach( $NEWCONFIG as $name=>$val )
	    {
			// Make values safe
			$val = preg_replace( "/'/", "\\'" , $val );
			$val = preg_replace( "/\r/", ""   , $val );
	        $val = str_replace( "\\", "\\\\"  , $val );

			$save[ $name ] = $val;
		}

        $DB->update($save, "dl_config");
    }
    
    function saveDBConfigKey($key, $value)
    {
        global $DB;
        
        $update = array("value" => $value);
        $DB->update($update, "dl_config", "`key`='{$key}'");
    }
    
    function saveChangedDBConfig($newvals = array())
    {
        global $DB, $CONFIG;
        foreach($newvals as $i=>$j)
        {
            if ( $CONFIG[$i] != $j )
            {
                $update = array("value" => $j);
                $DB->update($update, "dl_config", "`key`='{$i}'");
            }
        }
    }

    // Depricated
	function saveConfig($sitepath = "")
	{
	    global $CONFIG;
		ksort($CONFIG);
		foreach( $CONFIG as $name=>$val )
	    {
			// Make values safe
			$val = preg_replace( "/'/", "\\'" , $val );
			$val = preg_replace( "/\r/", ""   , $val );
	        $val = str_replace( "\\", "\\\\"  , $val );

			$save[ $name ] = $val;
		}

	    // Add PHP header to prevent file being read as text
		$saveString = '<?php'."\n";
	    // Add PHP config variables
	    foreach( $save as $name=>$val )
	    {
			$saveString .= '$CONFIG['."'".$name."'".'] = '."'".$val."';\n";
		}
	    // End PHP file
	    $saveString .= '?>';

        if ( $sitepath=="" )
            $sitepath = $CONFIG["sitepath"];
	    $fileName = $sitepath."/globalvars.php";
		if ( $fp = fopen( $fileName, 'w' ) )
		{
			fwrite($fp, $saveString, strlen($saveString) );
			fclose($fp);
			return true;
		}
		else
	    {
			$this->error("Could not create/save config file. Please ensure {$fileName} is set with write permissions [777/666]");
			return false;
	    }
	
	}

	function pages($items, $limit, $urlstring)
	{
        global $IN;
		$result = "";

        if ( !$IN['limit'] )
            $thispage = 1;
        else
            $thispage = ($IN['limit']/$limit)+1;

		$pages = ceil(($items/$limit));
		if ($pages > 1)
		{
			$result = GETLANG("pages").": ";
			for ($x=1; $x<=$pages; $x++)
			{
				if ($x > 1)
					$result .= ", ";
				$start = ($x - 1) * $limit;
				$result .= "<a href={$urlstring}&limit={$start}";
                if ( $thispage == $x )
                    $result .= " class='pagenumhi' ";
                else
                    $result .= " class='pagenum' ";
                $result .= ">{$x}</a>";
			}
		}	
		return $result;
	}
	
	function skinListBox($id=0, $type="", $boxname="skinchoice")
	{
		global $DB;
		
		$DB->query("SELECT * FROM dl_skinsets");
		
		if ( $myrow=$DB->fetch_row() )
		{
			$output = "<select name='{$boxname}'>";
			do
			{
				if ($type == "exclude" and $id == $myrow['setid'])
					continue;
				$output .= "<option value='{$myrow['setid']}'";
				if ( $myrow['setid'] == $id )
					$output .= " selected";
				$output .= ">{$myrow['name']}</option>";
			} while ( $myrow=$DB->fetch_row() );
			$output .= "</select>";
		}
		return $output;
	}
	
	function langListBox($id=0, $boxname="langchoice", $showAll=0)
	{
		global $DB;
		
		$DB->query("SELECT * FROM dl_langsets");
		
        $count = 0;
		if ( $myrow=$DB->fetch_row() )
		{
			$output = "<select name='{$boxname}'>";
			do
			{
                if ( !$showAll && !$myrow['visible'] )
                    continue;
                    
				$output .= "<option value='{$myrow['lid']}'";
				if ( $myrow['lid'] == $id )
					$output .= " selected";
				$output .= ">{$myrow['name']}</option>";
                
                $count++;
                
			} while ( $myrow=$DB->fetch_row() );
			$output .= "</select>";
		}
        if ( $count == 0 )
            return "";
		return $output;
	}
	
	function save_perms()
	{
		global $CONFIG, $IN;
		include_once ROOT_PATH."/engine/users.php";
		
		$guser = new user($CONFIG);
		$guser->initialise();
		
		$adminusers = "|";
		$modusers = "|";
		$memberusers = "|";
		$guestusers = "|";
	
		$table = $guser->mem_gtable;
		$result = $guser->userdb->query("SELECT * FROM `$table`");
			$this->error(GETLANG("er_users"));
		
		while ( $myrow = $guser->userdb->fetch_row($result) )
		{
			if ( $IN[$myrow[$guser->db_g_id]] == 1 )
				$adminusers .= $myrow[$guser->db_g_id]."|";
			if ( $IN[$myrow[$guser->db_g_id]] == 2 )
				$modusers .= $myrow[$guser->db_g_id]."|";
			if ( $IN[$myrow[$guser->db_g_id]] == 3 )
				$memberusers .= $myrow[$guser->db_g_id]."|";
			if ( $IN[$myrow[$guser->db_g_id]] == 4 )
				$guestusers .= $myrow[$guser->db_g_id]."|";
		}
		if ( $adminusers == "" )
		{
			$this->error( GETLANG("er_noadmingrp") );
			return;
		}
        
		saveDBConfigKey("adminperm", $adminusers);
		saveDBConfigKey("modperm", $modusers);
		saveDBConfigKey("memberperm", $memberusers);
		saveDBConfigKey("guestperm", $guestusers);
	}

	function emailvalidate($str)
	{ 
		// Submitted to phpfreaks.com by: Derek Ford - February 2nd, 2003
		$str = strtolower($str);
        // Don't allow spaces or commas or semi colons
        if ( stristr($str, ",") || stristr($str, " ") || stristr($str, ";"))
        {
            return 0;
        }
		if(ereg("^([^[:space:]]+)@(.+)\.(ad|ae|af|ag|ai|al|am|an|ao|aq|ar|arpa|as|at|au|aw|az|ba|bb|bd|be|bf|bg|bh|bi|bj|bm|bn|bo|br|bs|bt|bv|bw|by|bz|ca|cc|cd|cf|cg|ch|ci|ck|cl|cm|cn|co|com|cr|cu|cv|cx|cy|cz|de|dj|dk|dm|do|dz|ec|edu|ee|eg|eh|er|es|et|fi|fj|fk|fm|fo|fr|fx|ga|gb|gov|gd|ge|gf|gh|gi|gl|gm|gn|gp|gq|gr|gs|gt|gu|gw|gy|hk|hm|hn|hr|ht|hu|id|ie|il|in|int|io|iq|ir|is|it|jm|jo|jp|ke|kg|kh|ki|km|kn|kp|kr|kw|ky|kz|la|lb|lc|li|lk|lr|ls|lt|lu|lv|ly|ma|mc|md|mg|mh|mil|mk|ml|mm|mn|mo|mp|mq|mr|ms|mt|mu|mv|mw|mx|my|mz|na|nato|nc|ne|net|nf|ng|ni|nl|no|np|nr|nu|nz|om|org|pa|pe|pf|pg|ph|pk|pl|pm|pn|pr|pt|pw|py|qa|re|ro|ru|rw|sa|sb|sc|sd|se|sg|sh|si|sj|sk|sl|sm|sn|so|sr|st|sv|sy|sz|tc|td|tf|tg|th|tj|tk|tm|tn|to|tp|tr|tt|tv|tw|tz|ua|ug|uk|um|us|uy|uz|va|vc|ve|vg|vi|vn|vu|wf|ws|ye|yt|yu|za|zm|zw)$",$str)){ 
			return 1; 
		} 
		else
		{ 
			return 0; 
		} 
	} 
	
	function hasPerms($string, $id)
	{
		if ( !$string )
			return false;
	
		$toks = explode("|", $string);
		$num_toks = count($toks);
		$found = false;
		for ($i=0; $i < $num_toks; $i++ )
		{
			if ( $toks[$i] == $id )
			{
				$found = true;
				break;
			}
		}
		return $found;
	}
	
	function canSearch()
	{
		global $CONFIG, $guser, $DB;
	
		if ( $guser->getPermissions() & k_canSearch )
			return true;
		else
			return false;
	
		return false;
	}

    function canUploadFiles()
    {
        global $DB, $rwdInfo, $guser;

        if ( !$rwdInfo->cats_saved )
		{
			$DB->query("SELECT * FROM dl_categories");
			if ($myrow = $DB->fetch_row())
			{
				do
				{
					// Add category to cache
					$rwdInfo->cat_cache[$myrow["cid"]] = $myrow;
				} while ($myrow = $DB->fetch_row());
			}
			$rwdInfo->cats_saved = 1;
		}
        
		foreach( $rwdInfo->cat_cache as $cat )
		{
			if ( $this->canAccess($cat['cid'], "canUL") )
			{
				return TRUE;
			}
		}
        
        return FALSE;

    }

	function saveGlobals()
	{
	    $return = array();
	    if( !empty($_GET) )
	    {
		foreach( $_GET as $i=>$v )
		{	    
		    if( is_array($_GET[$i]) )
			{
			foreach( $_GET[$i] as $i2=>$v2 )
			{
				$return[$i][$i2] = $this->makeSafe($v2);
			}
		    }
		    else
		    {
			$return[$i] = $this->makeSafe($v);
		    }
		}
		}
	
	    // Post data is more securer so if anything has duplicates then use post instead
	    if( !empty($_POST) )
	    {
		foreach( $_POST as $i=>$v )
		{	    
		    if( is_array($_POST[$i]) )
		    {
			foreach( $_POST[$i] as $i2=>$v2 )
			{
				$return[$i][$i2] = $this->makeSafe($v2);
			}
		    }
		    else
		    {
			$return[$i] = $this->makeSafe($v);
		    }
		}
	    }
	
	    $return['ipaddr'] = $_SERVER['REMOTE_ADDR'];
	    $return['referer'] = $_SERVER['HTTP_REFERER'];

	    return $return;
	}
    
    function makeAlphaNumeric($in)
    {
        return preg_replace( "/[^a-zA-Z0-9\-\_\.]/", "" , $in );
    }
	
	// Make sent data safe from mallicious code
	function makeSafe($val)
	{
	    if ($val == "")
		return "";
	
	    // Trim whitespace
	    $val = trim($val);
		
	    $val = str_replace( "&#032;"       , " "		     , $val );
	    //$val = str_replace( chr(0xCA)      , ""		         , $val );
	    // Do a load of security checks on user input
	    $val = str_replace( "&"            , "&amp;"         , $val );
	    $val = str_replace( "<!--"         , "&#60;&#33;--"  , $val );
	    $val = str_replace( "-->"          , "--&#62;"       , $val );
	    $val = preg_replace( "/<script/i"  , "&#60;script"   , $val );
	    $val = str_replace( ">"            , "&gt;"          , $val );
	    $val = str_replace( "<"            , "&lt;"          , $val );
	    $val = str_replace( "\""           , "&quot;"        , $val );
	    $val = str_replace( "\\"           , "\\\\"          , $val );
	    //$val = preg_replace( "/\n/"        , "<br>"          , $val ); VV Bad!
	    $val = preg_replace( "/\\\$/"      , "&#036;"        , $val );
	    $val = preg_replace( "/\r/"        , ""              , $val );
	    $val = str_replace( "!"            , "&#33;"         , $val );
		$val = str_replace( "\'"            , "&#39;"         , $val );
	    $val = str_replace( "'"            , "&#39;"         , $val );
		$val = stripslashes($val);
		
		// Ensure unicode chars are OK
    	if ( 1 )
		{
			$val = preg_replace("/&amp;#([0-9]+);/s", "&#\\1;", $val );
		}
		
	    return $val;
	}
	
	function undoHTMLChars($t)
	{
		$t = str_replace( "&amp;" , "&", $t );
		$t = str_replace( "&lt;"  , "<", $t );
		$t = str_replace( "&gt;"  , ">", $t );
		$t = str_replace( "&quot;", '"', $t );
        $t = str_replace( "&#036;", '$', $t );
		//$t = str_replace( "&#39;" , "'", $t );
		
		return $t;
	}
	
	function my_filesize($size)
	{
		// Setup some common file size measurements.
		$kb = 1024;         // Kilobyte
		$mb = 1024 * $kb;   // Megabyte
		$gb = 1024 * $mb;   // Gigabyte
		$tb = 1024 * $gb;   // Terabyte
	
		// If it's less than a kb we just return the size, otherwise we keep going until
		// the size is in the appropriate measurement range
	
		if($size < $kb)
			return $size." B";
		else if($size < $mb)
			return round($size/$kb,2)." KB";
		else if($size < $gb)
			return round($size/$mb,2)." MB";
		else if($size < $tb)
			return round($size/$gb,2)." GB";
		else
			return round($size/$tb,2)." TB";
	}
	
	function calc_time ($seconds)
	{
		$days = (int)($seconds / 86400);
		$seconds -= ($days * 86400);
        $hours = 0;
        $minutes = 0;
		if ($seconds)
		{
			$hours = (int)($seconds / 3600);
			$seconds -= ($hours * 3600);
		}
		if ($seconds)
		{
			$minutes = (int)($seconds / 60);
			$seconds -= ($minutes * 60);
		}
		$time = array('days'=>(int)abs($days),
		'hours'=>(int)abs($hours),
		'minutes'=>(int)abs($minutes),
		'seconds'=>(int)abs($seconds));
		return $time;
	}
	
	function strip_ext($name)
	{
	     $ext = strrchr($name, '.');
	     if($ext !== false)
	     {
	         $name = substr($name, 0, -strlen($ext));
	     }
	     return $name;
	}

	function rw_setcookie($name, $value="", $rememberMe = 1, $addprefix=1)
	{
	    global $CONFIG;

        if ( $addprefix )
        {
            $name = $CONFIG['cookiePrefix'].$name;
        }
		$domain = $CONFIG['cookie_domain'] == "" ? ""  : $CONFIG['cookie_domain'];
	    $cookiepath = $CONFIG['cookie_path'] == "" ? "/" : $CONFIG['cookie_path'];

		if ($rememberMe)
	    {
			$time = time() + 3600 * 24 * 365;
	    }
	    
		setcookie($name, $value, $time, $cookiepath, $domain);
	}
	
	function rw_getcookie($name, $addprefix=1)
    {
        global $CONFIG;
        if ( $addprefix )
        {
            $name = $CONFIG['cookiePrefix'].$name;
        }
    	if (isset($_COOKIE[$name]))
    	{
    		return urldecode($_COOKIE[$name]);
    	}
    	else
    	{
    		return FALSE;
    	}
    }
	// Why is the standard function crap?
	function mynl2br( $data ) 
	{
	   return preg_replace( '!\\n!iU', "<br />", $data );
	}
	// Why isnt this a standard function?
	function br2nl( $data )
	{
	   return preg_replace( '!<br.*>!iU', "\n", $data );
	}

    function rmdirr($dirname)
    {
        // Sanity check
        if (!file_exists($dirname)) {
            return false;
        }

        // Simple delete for a file
        if (is_file($dirname)) {
            return unlink($dirname);
        }

        // Loop through the folder
        $dir = dir($dirname);
        while (false !== $entry = $dir->read()) {
            // Skip pointers
            if ($entry == '.' || $entry == '..') {
                continue;
            }

            // Recurse
            $this->rmdirr("$dirname/$entry");
        }

        // Clean up
        $dir->close();
        return rmdir($dirname);
    }
	
    function my_stripslashes($array)
    {
        if (!get_magic_quotes_gpc())
        {
            return $array;
        }
        if (is_string($array))
        {
            return stripslashes($array);
        }
        $new = array();
        foreach ($array as $key => $val)
        {
            if (is_array($val))
            {
                $new[$key] = $this->my_stripslashes($val);
            }
            else if (is_string($val))
            {
                $new[$key] = stripslashes($val);
            }
            else
                $new[$key] = $val;
        }
        return $new;
    }
	
	function shorten_string($string)
	{
		$varlength = strlen($string); // count number of characters
		$limit = 256; // set character limit
		if ($varlength > $limit)  // if character number if more than character limit
			$string = substr($string,0,$limit) . "..."; // display string up to character limit, add dots 
		return $string;
	}
	
	function isExternalFile($filename)
	{
		if ( stristr( $filename, "http://" ) || stristr( $filename, "ftp://" ) || stristr( $filename, "https://" ))
			return TRUE;
		else
			return FALSE;
	}

    function getCurrentLanguage()
    {
        global $CONFIG, $guser;

        if ( $guser->userdetails["lang"] and $guser->userdetails["lang"] != $CONFIG['defaultLang'] )
            $langpref = $guser->userdetails["lang"];
        else
            $langpref = $CONFIG['defaultLang'];
        return $langpref;
    }

    // seed with microseconds
    function make_seed()
    {
        list($usec, $sec) = explode(' ', microtime());
        return (float) $sec + ((float) $usec * 100000);
    }

    function add_data_to_logs($data=array())
    {
        global $DB;
        // TODO: Should security check this data but since it receives
        // no user entry it should be fine
        $DB->insert($data, "dl_logs");
    }

    function addErrorLog($error)
    {
        global $DB, $CONFIG, $IN, $guser;

        $userid = $guser->userid;
        if ( !$userid )
            $userid = 0;

        if ( $this->errorCount > 5 )
        {
            echo "Infinite loop! Last error was <br><pre>$error</pre>";
            exit();
        }
        $this->errorCount++;
        if ($CONFIG["logerrors"])
        {
            $data = array(  "type" => LOGTYPE_ERROR,
                            "referer" => addslashes($error),
                            "time" => time(),
                            "filename" => $_SERVER["HTTP_HOST"] . $_SERVER["REQUEST_URI"],
                            "IP" => $IN['ipaddr'],
                            "userid" => $userid);
            $this->add_data_to_logs($data);
        }

    }

    function addAdminLog($action="", $id=0)
    {
        global $DB, $CONFIG, $IN, $std, $guser;

        $userid = $guser->userid;
        if ( !$userid )
            $userid = 0;

        if ($CONFIG["logadminactions"] and $std->isUsingFullVersion())
        {
            $data = array(  "type" => LOGTYPE_ADMIN,
                            "referer" => addslashes($action),
                            "time" => time(),
                            "file" => $id,
                            "IP" => $IN['ipaddr'],
                            "userid" => $userid);
            $this->add_data_to_logs($data);
        }
    }
    
    function getSQLDebug()
    {
        global $CONFIG, $DB;
        
        $output = "<hr><b>Queries:</b><br>";
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
        return $output;
    }
    
    function debugArray($inArray)
    {
        echo "<pre>";
        print_r($inArray);
        echo "</pre>";
    }
}

?>