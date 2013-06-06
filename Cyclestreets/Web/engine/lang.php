<?php
/*********************************************************
 * $HeadURL: http://www.maidenfans.com/svn/rwdownload_new/TRUNK/engine/lang.php $
 * $Revision: 50 $
 * $LastChangedBy: realworld $
 * $LastChangedDate: 2008-12-20 20:32:52 +0000 (Sat, 20 Dec 2008) $
 *********************************************************/

class language
{
	var $lang,
		$langSet; 
	
	function language($set = "1")
    {
	}
    
    function loadLangFile($filename, $warnIfMissing=1)
    {
        global $rwdInfo, $std;
        
        $langpref = $std->getCurrentLanguage();
        if ( is_file(ROOT_PATH."/lang/{$langpref}/{$filename}.php") )
        {
            require_once (ROOT_PATH."/lang/{$langpref}/{$filename}.php");
            if ( !empty($lang) )
                $rwdInfo->lang = array_merge($rwdInfo->lang, $lang);
        }
        else if ( $warnIfMissing )
        {
            $std->warning(GETLANG("warn_langpackmissing")." ".$filename);
        }
    }
}

function GETLANG($element)
{
	global $rwdInfo;
	if (!$rwdInfo->lang[$element])
		return "#LANG.".strtoupper($element)."#";
	else
		return stripslashes($rwdInfo->lang[$element]);
}
?>