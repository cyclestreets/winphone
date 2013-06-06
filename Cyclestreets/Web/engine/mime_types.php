<?php
/*********************************************************
 * $HeadURL: http://www.maidenfans.com/svn/rwdownload_new/TRUNK/engine/mime_types.php $
 * $Revision: 50 $
 * $LastChangedBy: realworld $
 * $LastChangedDate: 2008-12-20 20:32:52 +0000 (Sat, 20 Dec 2008) $
 *********************************************************/
 
class Mime_Types
{
    var $mime_types = array(
        'txt'   => 'text/plain',
        'gif'   => 'image/gif',
        'jpg'   => 'image/jpeg',
        'html'  => 'text/html',
        'htm'   => 'text/html');
		
    var $file_cmd = '';
    var $file_options = array('b'=>null, 'i'=>null);
	
    function Mime_Types($mime_types=null)
    {
        if (is_string($mime_types)) {
            $this->load_file($mime_types);
        } elseif (is_array($mime_types)) {
            $this->set($mime_types);
        }
    }

    function scan($callback, &$param)
    {
        if (is_array($callback)) $method =& $callback[1];
        $mime_types = $this->mime_types;
        asort($mime_types);
        foreach ($mime_types as $ext => $type) {
            $ext_type = array($ext, $type);
            if (isset($method)) {
                $res = $callback[0]->$method($this, $ext_type, $param);
            } else {
                $res = $callback($this, $ext_type, $param);
            }
            if (!$res) return;
        }
    }

    function get_file_type($file, $use_ext=true)
    {
        $file = trim($file);
        if ($file == '') return false;
        $type = false;
        $result = false;
        if ($this->file_cmd and is_readable($file) and is_executable($this->file_cmd)) {
            $cmd = $this->file_cmd;
            foreach ($this->file_options as $option_key => $option_val) {
                $cmd .= ' -'.$option_key;
                if (isset($option_val)) $cmd .= ' '.escapeshellarg($option_val);
            }
            $cmd .= ' '.escapeshellarg($file);
            $result = @exec($cmd);
            if ($result) {
                $result = strtolower($result);
                $pattern = '[a-z0-9.+_-]';
                if (preg_match('!(('.$pattern.'+)/'.$pattern.'+)!', $result, $match)) {
                    if (in_array($match[2], array('application','audio','image','message',
                                                  'multipart','text','video','chemical','model')) ||
                            (substr($match[2], 0, 2) == 'x-')) {
                        $type = $match[1];
                    }
                }
            }
        }

        if (!$type and $use_ext and strpos($file, '.')) $type = $this->get_type($file);

        if (!$type and $result and preg_match('/\bascii\b/', $result)) $type = 'text/plain';
        return $type;
    }

    function get_type($ext)
    {
        $ext = strtolower($ext);
        // get position of last dot
        $dot_pos = strrpos($ext, '.');
        if ($dot_pos !== false) $ext = substr($ext, $dot_pos+1);
        if (($ext != '') and isset($this->mime_types[$ext])) return $this->mime_types[$ext];
        return false;
    }

    function set($type, $exts=null)
    {
        if (!isset($exts)) {
            if (is_array($type)) {
                foreach ($type as $mime_type => $exts) {
                    $this->set($mime_type, $exts);
                }
            }
            return;
        }
        if (!is_string($type)) return;

        $type = strtr(strtolower(trim($type)), ",;\t\r\n", '     ');
        if ($sp_pos = strpos($type, ' ')) $type = substr($type, 0, $sp_pos);

        if (!strpos($type, '/')) return;

        if (!is_array($exts)) $exts = explode(' ', $exts);
        foreach ($exts as $ext) {
            $ext = trim(str_replace('.', '', $ext));
            if ($ext == '') continue;
            $this->mime_types[strtolower($ext)] = $type;
        }
    }

    function has_extension($ext)
    {
        return (isset($this->mime_types[strtolower($ext)]));
    }

    function has_type($type)
    {
        return (in_array(strtolower($type), $this->mime_types));
    }

    function get_extension($type)
    {
        $type = strtolower($type);
        foreach ($this->mime_types as $ext => $m_type) {
            if ($m_type == $type) return $ext;
        }
        return false;
    }

    function get_extensions($type)
    {
        $type = strtolower($type);
        return (array_keys($this->mime_types, $type));
    }

    function remove_extension($exts)
    {
        if (!is_array($exts)) $exts = explode(' ', $exts);
        foreach ($exts as $ext) {
            $ext = strtolower(trim($ext));
            if (isset($this->mime_types[$ext])) unset($this->mime_types[$ext]);
        }
    }

    function remove_type($type=null)
    {
        if (!isset($type)) {
            $this->mime_types = array();
            return;
        }
        $slash_pos = strpos($type, '/');
        if (!$slash_pos) return;

        $type_info = array('last_match'=>false, 'wildcard'=>false, 'type'=>$type);
        if (substr($type, $slash_pos) == '/*') {
            $type_info['wildcard'] = true;
            $type_info['type'] = substr($type, 0, $slash_pos);
        }
        $this->scan(array(&$this, '_remove_type_callback'), $type_info);
    }

    function load_file($file)
    {
        if (!file_exists($file) || !is_readable($file)) return false;
        $data = file($file);
        foreach ($data as $line) {
            $line = trim($line);
            if (($line == '') || ($line == '#')) continue;
            $line = preg_split('/\s+/', $line, 2);
            if (count($line) < 2) continue;
            $exts = $line[1];
            $hash_pos = strpos($exts, '#');
            if ($hash_pos !== false) $exts = substr($exts, 0, $hash_pos);
            $this->set($line[0], $exts);
        }
        return true;
    }

    function _remove_type_callback(&$mime, $ext_type, $type_info)
    {

        $matched = false;
        list($ext, $type) = $ext_type;
        if ($type_info['wildcard']) {
            if (substr($type, 0, strpos($type, '/')) == $type_info['type']) {
                $matched = true;
            }
        } elseif ($type == $type_info['type']) {
            $matched = true;
        }
        if ($matched) {
            $this->remove_extension($ext);
            $type_info['last_match'] = true;
        } elseif ($type_info['last_match']) {
          
            return false;
        }
        return true;
    }        
}
?>