from torchvision import models
import torchvision.transforms as transforms
from torchsummary import summary
import torch
import model_irse
import skimage as sk
import numpy as np



model = model_irse.IR_50(input_size = [112, 112]) 

def de_preprocess(tensor):

    return tensor * 0.5 + 0.5


hflip = transforms.Compose([
            transforms.Normalize([0.5, 0.5, 0.5], [0.5, 0.5, 0.5])
        ])


def hflip_batch(imgs_tensor):
    hfliped_imgs = torch.empty_like(imgs_tensor)
    for i, img_ten in enumerate(imgs_tensor):
        hfliped_imgs[i] = hflip(img_ten)

    return hfliped_imgs




def load_model():
    model.load_state_dict(torch.load('model/ms1m_ir50/backbone_ir50_ms1m_epoch120.pth', map_location=torch.device('cpu')))
    model.eval()

load_model()
#print(model)
def get_image_face_embedding(image_filename):
    img = sk.io.imread(image_filename)/float(255)
    img = np.asarray(img).transpose(-1, 0, 1)
    print(img.shape)
    
    var_image = torch.tensor(np.expand_dims(img.astype(np.float32), axis=0)).type('torch.FloatTensor').to(torch.device('cpu'))
    
    print(var_image.shape)
    img2=hflip_batch(var_image)
    
    output = model.forward(var_image).detach().numpy()
    return output