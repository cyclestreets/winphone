<?php
/*********************************************************
 * $HeadURL: http://www.maidenfans.com/svn/rwdownload_new/TRUNK/engine/modloader.php $
 * $Revision: 50 $
 * $LastChangedBy: realworld $
 * $LastChangedDate: 2008-12-20 20:32:52 +0000 (Sat, 20 Dec 2008) $
 *********************************************************/

 // I doubt that this is very efficient but it is necessary as far as I can tell
class module_data
{
    var $modname;
    var $modbase;
    var $mod_files;
    var $mod_usercp;
    var $mod_html;
}

class modloader
{
    var $moddata = array();
    var $installed = array();
    var $newwmods = array();

    function modloader()
    {
        global $DB;
        $DB->query("SELECT modname,enabled FROM `dl_plugins`");
        while ($myrow = $DB->fetch_row())
        {
            $this->installed[$myrow['modname']] = $myrow;
        }
        $this->getAllModules();
    }

    function getAllModules()
    {
        global $rwdInfo, $guser, $std;

        $handle = opendir($rwdInfo->path."/modules/");
		while (($filename = readdir($handle)) !== false)
		{
			if (($filename != ".") and ($filename != ".."))
			{
                if ( is_dir($rwdInfo->path."/modules/".$filename) and file_exists($rwdInfo->path."/modules/".$filename."/module.php") )
				{
                    if ( !$this->installed[$filename] )
                    {
                        // found new module...
                        $this->newmods[] = $filename;
                        continue;
                    }
                    if ( $this->installed[$filename]['enabled'] != 1 )
                    {
                        // Disabled module.... continue
                        continue;
                    }
					$this->moddata[$filename]->modname = $filename;
                    require_once ($rwdInfo->path."/modules/{$filename}/module.php");
                    $this->moddata[$filename]->modbase = $mod_base;
                    if ( file_exists($rwdInfo->path."/modules/{$filename}/mod_files.php") )
                    {
                        require_once ($rwdInfo->path."/modules/{$filename}/mod_files.php");
                        $this->moddata[$filename]->mod_files = $mod_files;
                    }
                    if ( file_exists($rwdInfo->path."/modules/{$filename}/mod_usercp.php") )
                    {
                        require_once ($rwdInfo->path."/modules/{$filename}/mod_usercp.php");
                        $this->moddata[$filename]->mod_usercp = $mod_usercp;
                    }
                    if ( file_exists($rwdInfo->path."/modules/{$filename}/mod_html.php") )
                    {
                        require_once ($rwdInfo->path."/modules/{$filename}/mod_html.php");
                        $this->moddata[$filename]->mod_html = $mod_html;
                    }
                    // Pass module datascheme to module
                    $this->moddata[$filename]->modbase->init($this->moddata[$filename]);
				}
			}
		}

        if ( $guser->isAdmin && !empty($this->newmods) )
        {
            foreach ( $this->newmods as $i=>$j )
            {
                $std->info("Found new module $j. Click <a href='index.php?ACT=createplugin&name={$j}'>here</a> to install it.");
            }
        }
    }

    function loadLanguages()
    {
        global $rwdInfo;
        if ( !empty($this->moddata) )
        {
            foreach ( $this->moddata as $mod )
            {
                if ( $mod->modbase )
                {
                    $data = $mod->modbase->loadLanguage();
                }
            }
        }
    }

    function loadModule($modname="failed")
    {
        global $IN, $rwdInfo;
        $rwdInfo->base_url = "index.php?ACT={$IN['ACT']}&amp;name={$IN['name']}";
        $this->moddata[$modname]->modbase->loadModule();
    }
}

?>